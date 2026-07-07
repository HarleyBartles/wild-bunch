import { describe, expect, it } from 'vitest';
import { PathSegmentDto, TownProsperity } from '../api/types';

describe('PathSegmentDto', () => {
  it('stores coordinates', () => {
    const dto: PathSegmentDto = { startX: 10, startY: 20, endX: 30, endY: 40 };
    expect(dto.startX).toBe(10);
    expect(dto.startY).toBe(20);
    expect(dto.endX).toBe(30);
    expect(dto.endY).toBe(40);
  });
});

describe('TownProsperity', () => {
  it('has the correct enum values', () => {
    expect(TownProsperity.Boomtown).toBe(0);
    expect(TownProsperity.Prosperous).toBe(1);
    expect(TownProsperity.Poor).toBe(2);
    expect(TownProsperity.Destitute).toBe(3);
  });
});
