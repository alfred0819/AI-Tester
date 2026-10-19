import type { Fighter } from './Fighter';
import type { FrameInput } from '../input/InputSystem';
import { emptyInput } from '../input/InputSystem';

export class AIController {
  private thinkCooldown = 0;
  private intent: FrameInput = emptyInput();

  tick(self: Fighter, target: Fighter): FrameInput {
    if (self.isDead || target.isDead) return emptyInput();

    if (this.thinkCooldown > 0) {
      this.thinkCooldown--;
      return { ...this.intent, attackPressed: false, dashPressed: false, jumpPressed: false };
    }

    this.thinkCooldown = 8 + Math.floor(Math.random() * 12);
    this.intent = emptyInput();

    const dx = target.sprite.x - self.sprite.x;
    const dist = Math.abs(dx);

    if (dist > 120) {
      if (dx > 0) this.intent.right = true;
      else this.intent.left = true;
      if (Math.random() < 0.08) this.intent.dashPressed = true;
    } else if (dist < 60) {
      if (Math.random() < 0.3) {
        if (dx > 0) this.intent.left = true;
        else this.intent.right = true;
      }
    }

    if (dist < 100 && Math.random() < 0.45) {
      this.intent.attackPressed = true;
      if (self.isDashing || Math.random() < 0.2) {
        this.intent.dashPressed = true;
      }
    }

    if (Math.random() < 0.05 && dist > 80) {
      this.intent.jumpPressed = true;
    }

    return { ...this.intent };
  }
}
