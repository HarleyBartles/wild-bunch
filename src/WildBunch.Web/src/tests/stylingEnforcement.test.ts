import { describe, expect, it } from "vitest";
import { execSync } from "node:child_process";
import path from "node:path";

describe("Styling Enforcement", () => {
  it("ensures no components use legacy plain CSS classes from styles.css", () => {
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
      "case-modal__backdrop",
      "case-modal__header",
      "case-modal__body",
      "case-modal__state",
      "case-modal__grid",
      "case-modal__identity-grid",
      "case-modal__identity-suspects",
      "case-modal__section",
      "case-modal__section-head",
      "case-modal__card",
      "case-modal__stats",
      "case-modal__minor",
      "case-modal__anchor-list",
      "case-modal__lead-list",
      "case-modal__deductions",
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

    // Exclude 'tag' and 'stack' from simple grep if they are too noisy,
    // but they should be replaced by styled components now anyway.
    // Actually, we want to find className="tag" etc.

    const srcDir = path.resolve(__dirname, "..");
    
    // Use ripgrep via execSync to find any className="forbidden-class"
    // We escape the double quotes for the shell.
    // We look for exact matches within className="..."
    
    const violations: string[] = [];
    
    for (const cls of forbiddenClasses) {
      try {
        // Search for className="cls" or className="... cls ..."
        // This regex looks for the class name within a className string.
        const pattern = `className=["'][^"']*\\b${cls}\\b[^"']*["']`;
        const result = execSync(`rg -l "${pattern}" "${srcDir}" --glob "*.tsx" --glob "!tests/**"`, { encoding: "utf8" });
        if (result) {
          violations.push(`Class "${cls}" found in:\n${result}`);
        }
      } catch {
        // rg returns non-zero if no matches, which is fine
      }
    }

    expect(violations, `Found legacy CSS class violations:\n${violations.join("\n")}`).toHaveLength(0);
  });
});
