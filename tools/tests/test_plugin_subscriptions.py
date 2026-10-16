from __future__ import annotations

import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import check_plugin_subscriptions


def _files(tmp_path, plugin_path="./plugins/sample", activation=True, selector="ref"):
    (tmp_path / ".agents/plugins").mkdir(parents=True)
    (tmp_path / ".codex").mkdir()
    catalog = {
        "name": "wild-bunch",
        "plugins": [
            {
                "name": "sample",
                "source": {
                    "source": "git-subdir",
                    "url": "https://example.com/plugins.git",
                    "path": plugin_path,
                    selector: "main" if selector == "ref" else "a" * 40,
                },
            }
        ],
    }
    (tmp_path / ".agents/plugins/marketplace.json").write_text(
        json.dumps(catalog), encoding="utf-8"
    )
    enabled = "true" if activation else "false"
    (tmp_path / ".codex/config.toml").write_text(
        '[plugins."sample@wild-bunch"]\n'
        f"enabled = {enabled}\n\n"
        "[marketplaces.wild-bunch]\n"
        'source_type = "git"\nsource = "https://example.com/repo.git"\nref = "main"\n',
        encoding="utf-8",
    )
    return tmp_path


def test_valid_catalog_and_activation_pass(tmp_path):
    assert check_plugin_subscriptions.check_codex(_files(tmp_path)) == []


def test_inactive_catalog_plugin_remains_valid(tmp_path):
    assert check_plugin_subscriptions.check_codex(_files(tmp_path, activation=False)) == []


def test_immutable_sha_selector_is_supported(tmp_path):
    assert check_plugin_subscriptions.check_codex(_files(tmp_path, selector="sha")) == []


def test_traversing_plugin_path_fails(tmp_path):
    findings = check_plugin_subscriptions.check_codex(_files(tmp_path, "./plugins/../outside"))
    assert any("contained './'" in finding for finding in findings)


def test_missing_and_malformed_selectors_fail(tmp_path):
    root = _files(tmp_path)
    path = root / ".agents/plugins/marketplace.json"
    catalog = json.loads(path.read_text(encoding="utf-8"))
    source = catalog["plugins"][0]["source"]
    source.pop("ref")
    source["sha"] = "short"
    path.write_text(json.dumps(catalog), encoding="utf-8")
    assert any(
        "exactly one valid ref or full sha" in finding
        for finding in check_plugin_subscriptions.check_codex(root)
    )


def test_dangling_activation_fails(tmp_path):
    root = _files(tmp_path)
    (root / ".codex/config.toml").write_text(
        '[plugins."missing@wild-bunch"]\nenabled = true\n\n'
        '[marketplaces.wild-bunch]\nsource_type = "git"\n'
        'source = "https://example.com/repo.git"\n',
        encoding="utf-8",
    )
    assert any(
        "no matching catalog entry" in finding
        for finding in check_plugin_subscriptions.check_codex(root)
    )
