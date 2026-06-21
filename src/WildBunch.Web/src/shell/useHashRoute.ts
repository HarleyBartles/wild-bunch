import { useCallback, useEffect, useState } from "react";

function normalizeHash(rawHash: string): string {
  const trimmed = rawHash.replace(/^#/, "");
  if (trimmed === "" || trimmed === "/") {
    return "/";
  }

  return trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
}

function readCurrentHash(): string {
  if (typeof window === "undefined") {
    return "/";
  }

  return normalizeHash(window.location.hash);
}

export function useHashRoute(): { path: string; navigate: (path: string) => void } {
  const [path, setPath] = useState<string>(readCurrentHash);

  useEffect(() => {
    const handleHashChange = () => setPath(readCurrentHash());
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  const navigate = useCallback((nextPath: string) => {
    const target = normalizeHash(nextPath);
    if (normalizeHash(window.location.hash) === target) {
      return;
    }

    window.location.hash = target;
  }, []);

  return { path, navigate };
}
