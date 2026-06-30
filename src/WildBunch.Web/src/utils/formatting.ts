const storageKey = "wild-bunch.current-game-id";

export function formatMoney(value: number) {
  return `$${value.toFixed(2)}`;
}

export function readStoredGameId() {
  return window.localStorage.getItem(storageKey);
}

export { storageKey };
