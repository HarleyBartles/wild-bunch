import { describe, expect, it } from "vitest";
import { execSync } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import fs from "node:fs";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

describe("Styling Enforcement", () => {
  const srcDir = path.resolve(__dirname, "..");
  const webRoot = path.resolve(srcDir, "..");

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

    const violations: string[] = [];
    
    for (const cls of forbiddenClasses) {
      try {
        const pattern = `className=["'][^"']*\\b${cls}\\b[^"']*["']`;
        const result = execSync(`rg -l "${pattern}" "${srcDir}" --glob "*.tsx" --glob "!tests/**"`, { encoding: "utf8" });
        if (result) {
          violations.push(`Class "${cls}" found in:\n${result}`);
        }
      } catch {
        // no matches
      }
    }

    expect(violations, `Found legacy CSS class violations:\n${violations.join("\n")}`).toHaveLength(0);
  });
});
