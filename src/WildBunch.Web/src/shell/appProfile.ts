export type AppProfile = "public" | "playtest";

export function resolveAppProfile(environment: { dev: boolean; mode: string }): AppProfile {
  return environment.dev || environment.mode === "playtest" ? "playtest" : "public";
}
