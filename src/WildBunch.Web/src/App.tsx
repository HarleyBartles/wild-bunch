import { RouterProvider } from "@tanstack/react-router";
import { GameSessionProvider } from "./state/GameSessionProvider";
import { resolveAppProfile } from "./shell/appProfile";
import { createAppRouter } from "./shell/router";

const appProfile = resolveAppProfile({ dev: import.meta.env.DEV, mode: import.meta.env.MODE });
const router = createAppRouter(appProfile);

export default function App() {
  return (
    <GameSessionProvider>
      <RouterProvider router={router} />
    </GameSessionProvider>
  );
}
