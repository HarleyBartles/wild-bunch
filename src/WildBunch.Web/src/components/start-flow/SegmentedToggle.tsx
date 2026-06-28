import styled from "styled-components";

/**
 * A joined pill-shaped segmented toggle for small fixed option sets.
 *
 * One track, one sliding thumb, transparent label buttons on top.
 * The selected position is conveyed by the thumb geometry, not by
 * per-option backgrounds. Designed for 2–4 options; the thumb width
 * is derived from the option count.
 *
 * Portable: lives here for now but can be promoted to `ui/` as a
 * shared primitive when a second surface needs it.
 */
interface SegmentedToggleProps<T extends string | number> {
  options: ReadonlyArray<{ value: T; label: string }>;
  value: T;
  onSelect: (value: T) => void;
}

export function SegmentedToggle<T extends string | number>({
  options,
  value,
  onSelect,
}: SegmentedToggleProps<T>) {
  const selectedIndex = Math.max(
    0,
    options.findIndex((o) => o.value === value),
  );
  const count = options.length;

  return (
    <ToggleTrack role="group">
      <ToggleThumb $count={count} $index={selectedIndex} aria-hidden="true" />
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <ToggleLabel
            key={String(option.value)}
            type="button"
            $selected={selected}
            onClick={() => onSelect(option.value)}
            aria-pressed={selected}
          >
            {option.label}
          </ToggleLabel>
        );
      })}
    </ToggleTrack>
  );
}

const ToggleTrack = styled.div`
  position: relative;
  display: flex;
  width: 100%;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.03);
  padding: 3px;
  gap: 0;
  overflow: hidden;
`;

const ToggleThumb = styled.div<{ $count: number; $index: number }>`
  position: absolute;
  top: 3px;
  bottom: 3px;
  left: 3px;
  width: calc((100% - 6px) / ${({ $count }) => $count});
  border-radius: 999px;
  background: color-mix(in srgb, var(--accent) 22%, transparent);
  transform: translateX(calc(${({ $index }) => $index} * 100%));
  transition-property: transform;
  transition-duration: 0.18s;
  transition-timing-function: ease;

  @media (prefers-reduced-motion: reduce) {
    transition-duration: 0s;
  }
`;

const ToggleLabel = styled.button<{ $selected: boolean }>`
  position: relative;
  z-index: 1;
  flex: 1 1 0;
  min-width: 0;
  padding: 9px 10px;
  border: none;
  border-radius: 0;
  background: transparent;
  color: ${({ $selected }) =>
    $selected ? "var(--text)" : "color-mix(in srgb, var(--text) 65%, transparent)"};
  font-weight: 600;
  font-size: 0.88rem;
  cursor: pointer;
  white-space: nowrap;
  transition-property: color;
  transition-duration: 0.15s;
  transition-timing-function: ease;

  &:hover {
    color: var(--text);
  }

  &:focus-visible {
    outline: 2px solid color-mix(in srgb, var(--accent) 55%, transparent);
    outline-offset: -2px;
  }

  @media (max-width: 480px) {
    font-size: 0.8rem;
    padding: 8px 6px;
  }
`;
