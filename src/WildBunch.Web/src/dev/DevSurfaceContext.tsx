import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";

/**
 * The current gameplay surface for dev panel filtering.
 * Derived from game phase + active town place. The DevOverlay uses this
 * to decide which dev panels are contextually relevant.
 */
export type DevSurface =
  | "pre-session"
  | "town"
  | "saloon"
  | "sheriff"
  | "store"
  | "trailhead"
  | "trail"
  | "arrival";

const DevSurfaceContext = createContext<DevSurface>("pre-session");
const DevSurfaceSetterContext = createContext<(surface: DevSurface) => void>(() => {});

export function DevSurfaceProvider({ children }: { children: ReactNode }) {
  const [surface, setSurface] = useState<DevSurface>("pre-session");

  const setter = useCallback((next: DevSurface) => {
    setSurface((prev) => (prev === next ? prev : next));
  }, []);

  const value = useMemo(() => ({ surface }), [surface]);

  return (
    <DevSurfaceSetterContext.Provider value={setter}>
      <DevSurfaceContext.Provider value={value.surface}>
        {children}
      </DevSurfaceContext.Provider>
    </DevSurfaceSetterContext.Provider>
  );
}

export function useDevSurface(): DevSurface {
  return useContext(DevSurfaceContext);
}

export function useSetDevSurface(): (surface: DevSurface) => void {
  return useContext(DevSurfaceSetterContext);
}
