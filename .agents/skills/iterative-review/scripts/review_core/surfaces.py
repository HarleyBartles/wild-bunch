#!/usr/bin/env python3
"""Canonical changed-surface derivation from the bound unified diff.

``parse_diff_surfaces`` turns the exact ``git diff <merge-base> <head>`` bytes
materialized at enumeration into the deterministic changed-surface list that
coverage planning and reviewer context packages consume. The parser accepts
git's standard unified output only and fails closed on malformed file-section
headers, unparseable hunk headers, and truncated hunk bodies.

Each surface entry::

    {"path": "<repo-relative posix path>",
     "change_kind": "added"|"modified"|"deleted"|"renamed",
     "renamed_from": "<old path>"|None,
     "hunks": [{"old_start": int, "old_lines": int,
                "new_start": int, "new_lines": int}, ...]}

Entries are sorted by path and unique; a diff with zero file sections yields
an empty tuple. Binary files produce an entry with ``hunks: []``.
"""

from __future__ import annotations

import re

from . import model


class SurfaceParseError(Exception):
    """Raised when the bound diff cannot be parsed into surfaces."""


_HUNK_RE = re.compile(r"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")

# Extended header lines git may emit between `diff --git` and the content
# marker. Prefixes ending in a space carry a value; bare prefixes do not.
_EXTENDED_VALUE_PREFIXES = (
    "old mode ",
    "new mode ",
    "new file mode ",
    "deleted file mode ",
    "copy from ",
    "copy to ",
    "rename from ",
    "rename to ",
    "similarity index ",
    "dissimilarity index ",
    "index ",
)

_ESCAPES = {
    "a": "\a",
    "b": "\b",
    "f": "\f",
    "n": "\n",
    "r": "\r",
    "t": "\t",
    "v": "\v",
    "\\": "\\",
    '"': '"',
}


def _unquote(token: str) -> str:
    """Decode one git path token (bare or C-style quoted)."""
    if not token.startswith('"'):
        return token
    if len(token) < 2 or not token.endswith('"'):
        raise SurfaceParseError(f"unterminated quoted path {token!r}")
    body = token[1:-1]
    out: list[str] = []
    i = 0
    while i < len(body):
        c = body[i]
        if c != "\\":
            out.append(c)
            i += 1
            continue
        if i + 1 >= len(body):
            raise SurfaceParseError("trailing backslash in quoted path")
        esc = body[i + 1]
        if esc in _ESCAPES:
            out.append(_ESCAPES[esc])
            i += 2
        elif esc in "01234567":
            digits = body[i + 1 : i + 4]
            if len(digits) != 3 or not all(d in "01234567" for d in digits):
                raise SurfaceParseError(f"bad octal escape in path {token!r}")
            out.append(chr(int(digits, 8)))
            i += 4
        else:
            raise SurfaceParseError(f"bad escape \\{esc} in path {token!r}")
    return "".join(out)


def _read_token(s: str, i: int) -> tuple[str, int]:
    """Read one path token starting at s[i]; returns (raw_token, next_i)."""
    if s[i] == '"':
        j = i + 1
        while j < len(s):
            if s[j] == "\\":
                j += 2
                continue
            if s[j] == '"':
                return s[i : j + 1], j + 1
            j += 1
        raise SurfaceParseError("unterminated quoted path in diff --git header")
    j = s.find(" ", i)
    if j == -1:
        j = len(s)
    return s[i:j], j


def _split_git_paths(rest: str) -> tuple[str, str]:
    """Split the `a/x b/y` tail of a `diff --git` line into two paths."""
    tokens: list[str] = []
    i = 0
    while len(tokens) < 2:
        while i < len(rest) and rest[i] == " ":
            i += 1
        if i >= len(rest):
            raise SurfaceParseError(f"diff --git header needs two paths: {rest!r}")
        tok, i = _read_token(rest, i)
        tokens.append(tok)
    if rest[i:].strip():
        raise SurfaceParseError(f"trailing data in diff --git header: {rest!r}")
    old_tok, new_tok = (_unquote(t) for t in tokens)
    if not old_tok.startswith("a/") or not new_tok.startswith("b/"):
        raise SurfaceParseError(f"diff --git paths lack a//b/ prefixes: {rest!r}")
    return old_tok[2:], new_tok[2:]


def _marker_path(text: str) -> str:
    """Decode a `---`/`+++` marker: `/dev/null` or a quoted `a/`/`b/` path."""
    token = text.strip()
    if token == "/dev/null":
        return token
    return _unquote(token)


def _merge_kind(existing: str | None, new: str) -> str:
    if existing is not None and existing != new:
        raise SurfaceParseError(f"conflicting change-kind headers: {existing} vs {new}")
    return new


def _consume_hunks(lines: list[str], i: int) -> tuple[list[dict], int]:
    """Parse `@@` hunk headers plus their bodies starting at lines[i]."""
    hunks: list[dict] = []
    while i < len(lines) and lines[i].startswith("@@"):
        m = _HUNK_RE.match(lines[i])
        if m is None:
            raise SurfaceParseError(f"malformed hunk header: {lines[i]!r}")
        old_start = int(m.group(1))
        old_lines = int(m.group(2)) if m.group(2) is not None else 1
        new_start = int(m.group(3))
        new_lines = int(m.group(4)) if m.group(4) is not None else 1
        hunks.append(
            {
                "old_start": old_start,
                "old_lines": old_lines,
                "new_start": new_start,
                "new_lines": new_lines,
            }
        )
        i += 1
        o_need, n_need = old_lines, new_lines
        while i < len(lines):
            body = lines[i]
            if body.startswith("\\"):
                i += 1
                continue
            if o_need == 0 and n_need == 0:
                break
            if body == "" or body.startswith(" "):
                o_need -= 1
                n_need -= 1
            elif body.startswith("-"):
                o_need -= 1
            elif body.startswith("+"):
                n_need -= 1
            else:
                raise SurfaceParseError(f"unrecognized hunk body line: {body!r}")
            if o_need < 0 or n_need < 0:
                raise SurfaceParseError("hunk body overruns declared line counts")
            i += 1
        if o_need or n_need:
            raise SurfaceParseError("hunk body truncated vs declared line counts")
    return hunks, i


def _parse_file_section(lines: list[str], i: int) -> tuple[dict, int]:
    """Parse one file section beginning at a `diff --git ` line."""
    header = lines[i]
    old_path, new_path = _split_git_paths(header[len("diff --git ") :])
    i += 1
    saw_index = False
    kind_from_headers: str | None = None
    renamed_from: str | None = None
    while i < len(lines):
        line = lines[i]
        if (
            line.startswith("diff --git ")
            or line.startswith("--- ")
            or line.startswith("Binary files ")
            or line.startswith("GIT binary patch")
        ):
            break
        if not any(line.startswith(p) for p in _EXTENDED_VALUE_PREFIXES):
            raise SurfaceParseError(f"unrecognized extended header: {line!r}")
        if line.startswith("index "):
            saw_index = True
        elif line.startswith("new file mode "):
            kind_from_headers = _merge_kind(kind_from_headers, "added")
        elif line.startswith("deleted file mode "):
            kind_from_headers = _merge_kind(kind_from_headers, "deleted")
        elif line.startswith("rename from "):
            renamed_from = _unquote(line[len("rename from ") :])
            kind_from_headers = _merge_kind(kind_from_headers, "renamed")
        elif line.startswith("rename to "):
            kind_from_headers = _merge_kind(kind_from_headers, "renamed")
        elif line.startswith(("copy from ", "copy to ")):
            kind_from_headers = _merge_kind(kind_from_headers, "added")
        i += 1

    kind_from_markers: str | None = None
    hunks: list[dict] = []
    if i < len(lines) and lines[i].startswith("Binary files "):
        i += 1
    elif i < len(lines) and lines[i].startswith("GIT binary patch"):
        raise SurfaceParseError("GIT binary patch bodies are not accepted")
    elif i < len(lines) and lines[i].startswith("--- "):
        old_marker = _marker_path(lines[i][4:])
        i += 1
        if i >= len(lines) or not lines[i].startswith("+++ "):
            raise SurfaceParseError("'---' header without following '+++'")
        new_marker = _marker_path(lines[i][4:])
        i += 1
        if old_marker == "/dev/null":
            kind_from_markers = _merge_kind(kind_from_markers, "added")
        elif old_marker != "a/" + old_path:
            raise SurfaceParseError(f"'---' path {old_marker!r} diverges from diff --git a/ path")
        if new_marker == "/dev/null":
            kind_from_markers = _merge_kind(kind_from_markers, "deleted")
        elif new_marker != "b/" + new_path:
            raise SurfaceParseError(f"'+++' path {new_marker!r} diverges from diff --git b/ path")
        hunks, i = _consume_hunks(lines, i)
    elif saw_index:
        raise SurfaceParseError("'index' header without ---/+++ or Binary files marker")

    if kind_from_markers is not None and kind_from_headers is not None:
        _merge_kind(kind_from_headers, kind_from_markers)
    change_kind = kind_from_markers or kind_from_headers or "modified"
    entry = {
        "path": new_path,
        "change_kind": change_kind,
        "renamed_from": renamed_from,
        "hunks": hunks,
    }
    return entry, i


def parse_diff_surfaces(diff_text: str) -> tuple[dict, ...]:
    """Parse unified `git diff` output into the canonical changed-surface list."""
    if not isinstance(diff_text, str):
        raise SurfaceParseError("diff text must be a string")
    lines = diff_text.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    i = 0
    while i < len(lines) and not lines[i].startswith("diff --git "):
        i += 1
    entries: list[dict] = []
    while i < len(lines):
        if not lines[i].startswith("diff --git "):
            raise SurfaceParseError(f"expected 'diff --git' section, found: {lines[i]!r}")
        entry, i = _parse_file_section(lines, i)
        entries.append(entry)
    seen = set()
    for e in entries:
        if e["path"] in seen:
            raise SurfaceParseError(f"duplicate changed-surface path {e['path']!r}")
        seen.add(e["path"])
    entries.sort(key=lambda e: e["path"])
    return tuple(entries)


def surfaces_document(surfaces: tuple[dict, ...]) -> bytes:
    """Canonical JSON bytes for `surfaces.json` in the acquisition dir."""
    return model.canonical_json(list(surfaces))
