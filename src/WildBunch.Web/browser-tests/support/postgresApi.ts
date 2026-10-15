import { spawn, spawnSync } from "node:child_process";
import type { ChildProcess } from "node:child_process";
import { createServer } from "node:net";
import { Client } from "pg";

const apiPort = 5275;
const apiHealthUrl = `http://127.0.0.1:${apiPort}/health`;
const apiConnectionStringPrefix = "Host=localhost;Port=5435;Database=";

async function reserveAndReleaseApiPort(host: string): Promise<void> {
  const server = createServer();
  await new Promise<void>((resolve, reject) => {
    server.once("error", reject);
    server.listen(apiPort, host, () => {
      server.close((error) => (error ? reject(error) : resolve()));
    });
  });
}

async function assertApiPortIsAvailable(): Promise<void> {
  await reserveAndReleaseApiPort("127.0.0.1");
}

async function withAdminClient<T>(
  operation: (client: Client) => Promise<T>,
  database = "postgres",
): Promise<T> {
  const client = new Client({ connectionString: `postgres://postgres@localhost:5435/${database}` });
  await client.connect();
  try {
    return await operation(client);
  } finally {
    await client.end();
  }
}

function stopApi(api: ChildProcess): Promise<void> {
  const processId = api.pid;
  if (api.exitCode !== null || api.signalCode !== null || !processId) {
    return Promise.resolve();
  }

  return new Promise((resolve) => {
    const timeout = setTimeout(() => {
      if (process.platform === "win32") {
        spawnSync("taskkill.exe", ["/PID", String(processId), "/T", "/F"], { stdio: "ignore" });
      } else {
        try {
          process.kill(-processId, "SIGKILL");
        } catch {
          api.kill("SIGKILL");
        }
      }
      resolve();
    }, 10_000);

    api.once("exit", () => {
      clearTimeout(timeout);
      resolve();
    });

    if (process.platform === "win32") {
      spawnSync("taskkill.exe", ["/PID", String(processId), "/T", "/F"], { stdio: "ignore" });
    } else {
      try {
        process.kill(-processId, "SIGTERM");
      } catch {
        api.kill("SIGTERM");
      }
    }
  });
}

async function waitForApi(api: ChildProcess, readLogs: () => string): Promise<void> {
  const deadline = Date.now() + 120_000;
  while (Date.now() < deadline) {
    if (api.exitCode !== null || api.signalCode !== null) {
      throw new Error(`The isolated browser-test API exited early.\n${readLogs()}`);
    }

    if (!readLogs().includes(`Now listening on: http://127.0.0.1:${apiPort}`)) {
      await new Promise((resolve) => setTimeout(resolve, 100));
      continue;
    }

    try {
      const response = await fetch(apiHealthUrl);
      if (response.ok) return;
    } catch {
      // The API is still starting or applying its database migrations.
    }

    await new Promise((resolve) => setTimeout(resolve, 500));
  }

  throw new Error(`The isolated browser-test API did not become healthy.\n${readLogs()}`);
}

async function dropDatabase(databaseName: string): Promise<void> {
  await withAdminClient(async (client) => {
    await client.query(
      "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = $1",
      [databaseName],
    );
    await client.query(`DROP DATABASE IF EXISTS "${databaseName}"`);
  });
}

export async function startIsolatedApi(): Promise<() => Promise<void>> {
  await assertApiPortIsAvailable();
  const databaseName = `wildbunch_browser_${process.pid}_${crypto.randomUUID().replace(/-/g, "")}`;
  await withAdminClient((client) => client.query(`CREATE DATABASE "${databaseName}"`));

  const api = spawn(
    "dotnet",
    [
      "run",
      "--project",
      "../WildBunch.Api/WildBunch.Api.csproj",
      "--no-launch-profile",
      "--",
      // Command-line configuration outranks the shared database environment set by the task runner.
      `--ConnectionStrings:WildBunchPostgresDb=${apiConnectionStringPrefix}${databaseName};Username=postgres`,
    ],
    {
      cwd: process.cwd(),
      detached: process.platform !== "win32",
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: "Development",
        ASPNETCORE_URLS: "http://127.0.0.1:5275",
      },
      stdio: ["ignore", "pipe", "pipe"],
    },
  );

  let logs = "";
  const appendLog = (chunk: Buffer) => {
    logs = `${logs}${chunk.toString()}`.slice(-16_000);
  };
  api.stdout?.on("data", appendLog);
  api.stderr?.on("data", appendLog);

  try {
    await waitForApi(api, () => logs);
    const activeSessions = await withAdminClient(
      (client) => client.query('SELECT 1 FROM "GameSessions" WHERE "Status" = \'Active\' LIMIT 1'),
      databaseName,
    );
    if (activeSessions.rowCount !== 0) {
      throw new Error(`The browser-test disposable database ${databaseName} was not empty.`);
    }
  } catch (error) {
    await stopApi(api);
    await dropDatabase(databaseName);
    throw error;
  }

  return async () => {
    await stopApi(api);
    await dropDatabase(databaseName);
  };
}
