import { describe, expect, it } from 'vitest';
import { HEAVY_ATTACK, HEAVY_MULTIPLIER, LIGHT_ATTACK, resolveDamage } from '../config/combat';

describe('combat config', () => {
  it('heavy attack deals double light damage', () => {
    expect(HEAVY_ATTACK.damage).toBe(LIGHT_ATTACK.damage * HEAVY_MULTIPLIER);
    expect(resolveDamage(true)).toBe(20);
    expect(resolveDamage(false)).toBe(10);
  });
});
