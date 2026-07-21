import { describe, expect, it } from "vitest";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";
import { tmpdir } from "node:os";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function buildLegacyClassPattern(classes: readonly string[]): string {
  const alternatives = classes.map(escapeRegex).join("|");
  return `className=["'](?:[^"']*\\s)?(?:${alternatives})(?:\\s|["'])`;
}

function findMatchesInTsxFiles(directory: string, matcher: RegExp): string {
  return findMatchingLines(directory, matcher).join("\n");
}

function findMatchingLines(directory: string, matcher: RegExp): string[] {
  return fs.readdirSync(directory, { withFileTypes: true })
    .flatMap((entry) => {
      const entryPath = path.join(directory, entry.name);

      if (entry.isDirectory()) {
        return entry.name === "tests" ? [] : findMatchingLines(entryPath, matcher);
      }

      if (!entry.isFile() || !entry.name.endsWith(".tsx")) {
        return [];
      }

      return fs.readFileSync(entryPath, "utf8")
        .split("\n")
        .flatMap((line, index) => matcher.test(line) ? [`${entryPath}:${index + 1}:${line}`] : []);
    });
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

  it("finds matching lines without requiring an external search executable", () => {
    const fixtureDirectory = fs.mkdtempSync(path.join(tmpdir(), "wild-bunch-styling-"));
    const fixturePath = path.join(fixtureDirectory, "Example.tsx");

    try {
      fs.writeFileSync(fixturePath, 'export const Example = () => <div className="panel" />;\n');

      const matches = findMatchesInTsxFiles(fixtureDirectory, /className="panel"/);

      expect(matches).toContain("Example.tsx:1:");
    } finally {
      fs.rmSync(fixtureDirectory, { force: true, recursive: true });
    }
  });

  it("does not report blank matches from nested directories", () => {
    const fixtureDirectory = fs.mkdtempSync(path.join(tmpdir(), "wild-bunch-styling-"));
    const nestedDirectory = path.join(fixtureDirectory, "components", "shared");
    const siblingDirectory = path.join(fixtureDirectory, "hooks");

    try {
      fs.mkdirSync(nestedDirectory, { recursive: true });
      fs.mkdirSync(siblingDirectory);
      fs.writeFileSync(path.join(nestedDirectory, "Example.tsx"), "export const Example = () => null;\n");
      fs.writeFileSync(path.join(siblingDirectory, "useExample.tsx"), "export const useExample = () => null;\n");

      expect(findMatchesInTsxFiles(fixtureDirectory, /className="panel"/)).toBe("");
    } finally {
      fs.rmSync(fixtureDirectory, { force: true, recursive: true });
    }
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
    const result = findMatchesInTsxFiles(srcDir, /import\s+['"].*\.css['"]/);

    expect(result, `Found .css imports in TSX files:\n${result}`).toBe("");
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

    const result = findMatchesInTsxFiles(
      srcDir,
      new RegExp(buildLegacyClassPattern(forbiddenClasses)),
    );

    expect(result, `Found legacy CSS class violations:\n${result}`).toBe("");
  });

  it("ensures no inline style props remain in migrated component files", () => {
    // Inline style={{ ... }} props are forbidden in component files.
    // All static layout/spacing/typography must live in styled components.
    // The only allowed exception is for genuinely dynamic values that cannot
    // be known at styling time — those should use transient $props instead.
    const result = findMatchesInTsxFiles(srcDir, /style=\{\{/);

    expect(result, `Found inline style={{ ... }} props in TSX files:\n${result}`).toBe("");
  });
});
