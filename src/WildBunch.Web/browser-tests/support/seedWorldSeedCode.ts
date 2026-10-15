import { execFileSync } from "node:child_process";
import { resolve } from "node:path";

export function seedWorldSeedCode(): string {
  const project = resolve(
    process.cwd(),
    "browser-tests/support/seed-code/SeedWorldSeedCode.csproj",
  );
  const output = execFileSync(
    "dotnet",
    ["run", "--project", project, "--no-launch-profile", "--verbosity", "quiet"],
    { encoding: "utf8" },
  );
  const seedCode = output.trim().split(/\r?\n/).at(-1);
  if (!seedCode || !/^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(seedCode)) {
    throw new Error(`The SeedWorld seed-code helper did not return a UUID: ${output}`);
  }
  return seedCode;
}
