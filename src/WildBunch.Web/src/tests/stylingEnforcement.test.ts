import { ESLint } from "eslint";
import { describe, expect, it } from "vitest";

const eslint = new ESLint({ cwd: process.cwd() });

async function stylingDiagnostics(source: string) {
  const [result] = await eslint.lintText(source, {
    filePath: "src/tests/AppShell.test.tsx",
  });

  return result.messages
    .filter((message) => message.ruleId === "no-restricted-syntax")
    .map(({ message, severity }) => ({ message, severity }));
}

describe("web styling policy", () => {
  it("rejects authored className styling hooks through ESLint", async () => {
    const diagnostics = await stylingDiagnostics(
      'export const Example = () => <div className="legacy-panel" />;',
    );

    expect(diagnostics).toEqual([
      { message: "Use styled-components instead of a className styling hook.", severity: 2 },
    ]);
  });

  it("rejects inline style props through ESLint", async () => {
    const diagnostics = await stylingDiagnostics(
      'export const Example = () => <div style={{ color: "red" }} />;',
    );

    expect(diagnostics).toEqual([
      { message: "Use styled-components instead of an inline style prop.", severity: 2 },
    ]);
  });

  it("allows styled-components to own component styling", async () => {
    const diagnostics = await stylingDiagnostics(`
      import styled from "styled-components";

      type SurfaceProps = { $active: boolean };

      export const Surface = styled.section<SurfaceProps>\`
        color: \${({ $active }) => ($active ? "tomato" : "slategray")};
      \`;
    `);

    expect(diagnostics).toEqual([]);
  });

  it("ignores className and style text outside JSX attributes", async () => {
    const diagnostics = await stylingDiagnostics(`
      const policyNames = ["className", "style"];
      export const Example = () => <div data-testid={policyNames.join("-")} />;
    `);

    expect(diagnostics).toEqual([]);
  });
});
