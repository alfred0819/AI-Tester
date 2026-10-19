import Phaser from 'phaser';
import { INPUT_BUFFER_FRAMES } from '../config/combat';
import { KEY_CODES } from '../config/controls';

export interface FrameInput {
  left: boolean;
  right: boolean;
  down: boolean;
  jump: boolean;
  attack: boolean;
  dash: boolean;
  attackPressed: boolean;
  dashPressed: boolean;
  jumpPressed: boolean;
}

export class InputSystem {
  private keys!: Record<keyof typeof KEY_CODES, Phaser.Input.Keyboard.Key>;

  private attackBuffer = 0;
  private dashBuffer = 0;
  private wasDashingLastFrame = false;

  constructor(scene: Phaser.Scene) {
    const kb = scene.input.keyboard!;
    this.keys = {
      up: kb.addKey(KEY_CODES.up),
      left: kb.addKey(KEY_CODES.left),
      down: kb.addKey(KEY_CODES.down),
      right: kb.addKey(KEY_CODES.right),
      attack: kb.addKey(KEY_CODES.attack),
      jump: kb.addKey(KEY_CODES.jump),
      dash: kb.addKey(KEY_CODES.dash),
    };
  }

  poll(isOpponentDashing: boolean): FrameInput {
    const attackDown = this.keys.attack.isDown;
    const dashDown = this.keys.dash.isDown;

    const attackJust = Phaser.Input.Keyboard.JustDown(this.keys.attack);
    const dashJust = Phaser.Input.Keyboard.JustDown(this.keys.dash);
    const jumpJust = Phaser.Input.Keyboard.JustDown(this.keys.jump);

    if (attackJust) this.attackBuffer = INPUT_BUFFER_FRAMES;
    if (dashJust) this.dashBuffer = INPUT_BUFFER_FRAMES;

    if (this.attackBuffer > 0) this.attackBuffer--;
    if (this.dashBuffer > 0) this.dashBuffer--;

    this.wasDashingLastFrame = isOpponentDashing;

    return {
      left: this.keys.left.isDown,
      right: this.keys.right.isDown,
      down: this.keys.down.isDown,
      jump: this.keys.jump.isDown,
      attack: attackDown,
      dash: dashDown,
      attackPressed: attackJust || this.attackBuffer === INPUT_BUFFER_FRAMES,
      dashPressed: dashJust || this.dashBuffer === INPUT_BUFFER_FRAMES,
      jumpPressed: jumpJust,
    };
  }

  /** 冲刺窗口内 buffered 的攻击意图 */
  consumeAttackIntent(isDashing: boolean): boolean {
    if (!isDashing && !this.wasDashingLastFrame) return false;
    if (this.attackBuffer <= 0 && !Phaser.Input.Keyboard.JustDown(this.keys.attack)) {
      return false;
    }
    return this.keys.attack.isDown || this.attackBuffer > 0;
  }

  clearAttackBuffer(): void {
    this.attackBuffer = 0;
  }
}

/** CPU 虚拟输入 */
export function emptyInput(): FrameInput {
  return {
    left: false,
    right: false,
    down: false,
    jump: false,
    attack: false,
    dash: false,
    attackPressed: false,
    dashPressed: false,
    jumpPressed: false,
  };
}
