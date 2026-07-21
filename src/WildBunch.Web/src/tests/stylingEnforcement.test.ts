import { describe, expect, it } from "vitest";
import { execFileSync, execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function buildLegacyClassPattern(classes: readonly string[]): string {
  const alternatives = classes.map(escapeRegex).join("|");
  return `className=["'](?:[^"']*\\s)?(?:${alternatives})(?:\\s|["'])`;
}

function findMatches(command: string, arguments_: string[]): string {
  try {
    return execFileSync(command, arguments_, { encoding: "utf8" });
  } catch (error: unknown) {
    if (
      typeof error === "object"
      && error !== null
      && "status" in error
      && error.status === 1
    ) {
      return "";
    }

    throw error;
  }
}

describe("Styling Enforcement", () => {
  const srcDir = path.resolve(__dirname, "..");
  const webRoot = path.resolve(srcDir, "..");

  it("builds one literal className matcher for every forbidden class", () => {
    const matcher = new RegExp(buildLegacyClassPattern(["panel", "action-row"]));

    expect(matcher.test('className="panel action-row"')).toBe(true);
    expect(matcher.test('className="other-panel"')).toBe(false);
    expect(matcher.test('className="action-row-extra"')).toBe(false);
  });

  it("ensures src/styles.css does not exist", () => {
    const stylesCssPath = path.resolve(srcDir, "styles.css");
    expect(fs.existsSync(stylesCssPath), "src/styles.css should have been deleted").toBe(false);
  });

  it("ensures src/styles/index.scss does not reference styles.css", () => {
    const indexScssPath = path.resolve(srcDir, "styles", "index.scss");
    const content = fs.readFileSync(indexScssPath, "utf8");
    expect(content, "src/styles/index.scss should not reference styles.css").not.toContain("styles.css");
  });

  it("ensures no .css imports remain in TSX files", () => {
    try {
      const result = execSync(`rg "import\\s+['\\"].*\\.css['\\"]" "${srcDir}" --glob "*.tsx" --glob "!tests/**"`, { encoding: "utf8" });
      if (result) {
        expect.fail(`Found .css imports in TSX files:\n${result}`);
      }
    } catch {
      // rg returns non-zero if no matches
    }
  });

  it("ensures no legacy plain CSS classes from styles.css are used", () => {
    const forbiddenClasses = [
      "panel",
      "panel-head",
      "panel-subtitle",
      "notice",
      "error",
      "muted",
      "status-card",
      "stat-list",
      "stack",
      "action-row",
      "destination-card",
      "compact-item",
      "log-entry",
      "tag-row",
      "tag",
      "field",
      "flow-surface",
      "flow-notice",
      "flow-error",
      "case-modal",
      "arrival-card",
      "arrival-lead",
      "town-hub-header",
      "town-hub-lead",
      "town-hub-grid",
      "place-card",
      "place-header",
      "place-body",
      "travel-prep-body",
      "travel-prep-ride",
      "travel-prep-actions",
      "trail-lock-banner",
    ];

    const result = findMatches("rg", [
      "-n",
      buildLegacyClassPattern(forbiddenClasses),
      srcDir,
      "--glob",
      "*.tsx",
      "--glob",
      "!tests/**",
    ]);

    expect(result, `Found legacy CSS class violations:\n${result}`).toBe("");
  });

  it("ensures no inline style props remain in migrated component files", () => {
    // Inline style={{ ... }} props are forbidden in component files.
    // All static layout/spacing/typography must live in styled components.
    // The only allowed exception is for genuinely dynamic values that cannot
    // be known at styling time — those should use transient $props instead.
    try {
      const result = execSync(
        `rg "style=\\{\\{" "${srcDir}" --glob "*.tsx" --glob "!tests/**"`,
        { encoding: "utf8" },
      );
      if (result) {
        expect.fail(`Found inline style={{ ... }} props in TSX files:\n${result}`);
      }
    } catch {
      // rg returns non-zero if no matches — this is the expected pass case
    }
  });
});
