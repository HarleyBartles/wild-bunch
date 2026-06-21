import { AppShell } from "./shell/AppShell";
import { GameSessionProvider } from "./state/GameSessionContext";

export default function App() {
  return (
    <GameSessionProvider>
      <AppShell />
    </GameSessionProvider>
  );
}
