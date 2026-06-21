import { RouterProvider } from "@tanstack/react-router";
import { GameSessionProvider } from "./state/GameSessionProvider";
import { router } from "./shell/router";

export default function App() {
  return (
    <GameSessionProvider>
      <RouterProvider router={router} />
    </GameSessionProvider>
  );
}
