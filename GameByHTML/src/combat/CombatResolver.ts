import { Fighter } from '../entities/Fighter';

export class CombatResolver {
  static resolve(a: Fighter, b: Fighter): void {
    if (a.canDealHit()) {
      const hit = a.getActiveHitbox();
      const hurt = b.getHurtbox();
      if (hit && a.overlapsHit(hurt)) {
        b.receiveHit(a);
      }
    }
    if (b.canDealHit()) {
      const hit = b.getActiveHitbox();
      const hurt = a.getHurtbox();
      if (hit && b.overlapsHit(hurt)) {
        a.receiveHit(b);
      }
    }
  }
}
