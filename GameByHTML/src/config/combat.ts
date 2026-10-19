export const HEAVY_MULTIPLIER = 2;

export const MAX_HP = 100;

export const INPUT_BUFFER_FRAMES = 4;

export const DASH = {
  durationFrames: 12,
  cooldownFrames: 30,
  speed: 12,
} as const;

export const LIGHT_ATTACK = {
  startup: 4,
  active: 3,
  recovery: 8,
  damage: 10,
  hitstun: 12,
  knockbackSpeed: 4,
  knockbackFrames: 8,
  hitbox: { w: 48, h: 32, offsetX: 36, offsetY: -16 },
} as const;

export const HEAVY_ATTACK = {
  startup: 6,
  active: 4,
  recovery: 12,
  damage: LIGHT_ATTACK.damage * HEAVY_MULTIPLIER,
  hitstun: 20,
  knockbackSpeed: 6,
  knockbackFrames: 12,
  hitbox: { w: 64, h: 40, offsetX: 44, offsetY: -20 },
} as const;

export interface MoveDef {
  startup: number;
  active: number;
  recovery: number;
  damage: number;
  hitstun: number;
  knockbackSpeed: number;
  knockbackFrames: number;
  hitbox: { w: number; h: number; offsetX: number; offsetY: number };
}

export function resolveDamage(isHeavy: boolean): number {
  return isHeavy ? HEAVY_ATTACK.damage : LIGHT_ATTACK.damage;
}

export function getMove(isHeavy: boolean): MoveDef {
  return isHeavy ? HEAVY_ATTACK : LIGHT_ATTACK;
}
