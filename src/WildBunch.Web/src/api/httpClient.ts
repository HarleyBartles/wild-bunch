const defaultApiBaseUrl = "http://localhost:5275";

export function getApiBaseUrl() {
  const configured = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (configured === undefined) {
    return defaultApiBaseUrl;
  }

  if (configured === "") {
    return "";
  }

  return configured.replace(/\/+$/, "");
}

export async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  const contentType = response.headers.get("content-type") ?? "";
  const body = contentType.includes("json")
    ? await response.json().catch(() => null)
    : await response.text().catch(() => "");

  if (!response.ok) {
    const message = extractErrorMessage(body) || `Request failed with status ${response.status}`;
    throw new Error(message);
  }

  return body as T;
}

function extractErrorMessage(body: unknown) {
  if (typeof body === "string") {
    return body;
  }

  if (!body || typeof body !== "object") {
    return "";
  }

  const problem = body as Record<string, unknown>;
  if (typeof problem.title === "string" && problem.title.trim()) {
    return problem.title;
  }

  const errors = problem.errors;
  if (errors && typeof errors === "object") {
    for (const value of Object.values(errors as Record<string, unknown>)) {
      if (Array.isArray(value) && value.length > 0 && typeof value[0] === "string") {
        return value[0];
      }
    }
  }

  if (typeof problem.detail === "string" && problem.detail.trim()) {
    return problem.detail;
  }

  return "";
}
