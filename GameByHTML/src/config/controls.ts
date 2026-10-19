/** 键位单一数据源 — 与 docs/combat-design.md 同步 */
export const CONTROL_MAP = {
  up: 'W',
  left: 'A',
  down: 'S',
  right: 'D',
  attack: 'J',
  jump: 'K',
  dash: 'L',
} as const;

/** Phaser KeyCodes 数值（WASD + JKL） */
export const KEY_CODES = {
  up: 87,
  left: 65,
  down: 83,
  right: 68,
  attack: 74,
  jump: 75,
  dash: 76,
} as const;

export type ControlAction = keyof typeof CONTROL_MAP;
