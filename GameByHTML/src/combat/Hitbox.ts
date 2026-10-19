export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export function rectsOverlap(a: Rect, b: Rect): boolean {
  return (
    a.x < b.x + b.w &&
    a.x + a.w > b.x &&
    a.y < b.y + b.h &&
    a.y + a.h > b.y
  );
}

export function worldHitbox(
  originX: number,
  originY: number,
  facing: 1 | -1,
  def: { w: number; h: number; offsetX: number; offsetY: number },
): Rect {
  const ox = facing === 1 ? def.offsetX : -def.offsetX - def.w;
  return {
    x: originX + ox,
    y: originY + def.offsetY,
    w: def.w,
    h: def.h,
  };
}

export function hurtbox(
  originX: number,
  originY: number,
  bodyW: number,
  bodyH: number,
  crouching: boolean,
): Rect {
  const h = crouching ? bodyH * 0.55 : bodyH;
  const yOff = crouching ? bodyH - h : 0;
  return {
    x: originX - bodyW / 2,
    y: originY - bodyH + yOff,
    w: bodyW,
    h,
  };
}
