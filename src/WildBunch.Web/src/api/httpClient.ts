const defaultApiBaseUrl = "http://localhost:5275";

export class HttpRequestError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "HttpRequestError";
  }
}

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
  const headers = new Headers({ "Content-Type": "application/json" });
  new Headers(init?.headers).forEach((value, name) => headers.set(name, value));

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
  });

  const contentType = response.headers.get("content-type") ?? "";
  const body = contentType.includes("json")
    ? await response.json().catch(() => null)
    : await response.text().catch(() => "");

  if (!response.ok) {
    const message = extractErrorMessage(body) || `Request failed with status ${response.status}`;
    throw new HttpRequestError(message, response.status);
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

  if (typeof problem.error === "string" && problem.error.trim()) {
    return problem.error;
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
