export const GAME_WIDTH = 1280;
export const GAME_HEIGHT = 720;

export const STAGE = {
  floorY: 620,
  leftBound: 80,
  rightBound: 1200,
} as const;

export const PHYSICS = {
  gravityY: 900,
  walkSpeed: 5,
  airWalkFactor: 0.7,
  jumpVelocity: -420,
} as const;

export const ROUND_TIME_SEC = 99;
