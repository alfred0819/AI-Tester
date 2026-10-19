import Phaser from 'phaser';
import { MAX_HP } from '../config/combat';
import { GAME_HEIGHT, GAME_WIDTH, PHYSICS, ROUND_TIME_SEC, STAGE } from '../config/game';
import { CONTROL_MAP } from '../config/controls';
import { gameEvents } from '../core/EventBus';
import { CombatResolver } from '../combat/CombatResolver';
import { Fighter } from '../entities/Fighter';
import { AIController } from '../entities/AIController';
import { InputSystem } from '../input/InputSystem';

export class FightScene extends Phaser.Scene {
  private p1!: Fighter;
  private p2!: Fighter;
  private inputSystem!: InputSystem;
  private ai = new AIController();
  private hpBarP1!: Phaser.GameObjects.Graphics;
  private hpBarP2!: Phaser.GameObjects.Graphics;
  private timerText!: Phaser.GameObjects.Text;
  private debugGfx!: Phaser.GameObjects.Graphics;
  private timeLeft = ROUND_TIME_SEC;
  private ended = false;
  private showDebug = false;

  constructor() {
    super({ key: 'Fight' });
  }

  create(): void {
    this.ended = false;
    this.timeLeft = ROUND_TIME_SEC;

    this.cameras.main.setBackgroundColor('#2d3436');
    this.physics.world.gravity.y = PHYSICS.gravityY;

    const floor = this.add.rectangle(GAME_WIDTH / 2, STAGE.floorY + 20, GAME_WIDTH, 40, 0x636e72);
    floor.setOrigin(0.5, 0);

    this.p1 = new Fighter(this, 'p1', 400, 0x0984e3, MAX_HP);
    this.p2 = new Fighter(this, 'p2', 880, 0xd63031, MAX_HP);

    const ground = this.add.rectangle(GAME_WIDTH / 2, STAGE.floorY, GAME_WIDTH, 8, 0x000000, 0);
    this.physics.add.existing(ground, true);
    const groundBody = ground.body as Phaser.Physics.Arcade.StaticBody;
    groundBody.setSize(GAME_WIDTH, 8);

    this.physics.add.collider(this.p1.sprite, ground);
    this.physics.add.collider(this.p2.sprite, ground);

    this.inputSystem = new InputSystem(this);

    this.hpBarP1 = this.add.graphics();
    this.hpBarP2 = this.add.graphics();
    this.timerText = this.add.text(GAME_WIDTH / 2, 36, '', {
      fontSize: '28px',
      color: '#ffffff',
    }).setOrigin(0.5);

    this.add.text(16, GAME_HEIGHT - 32, `操作: ${CONTROL_MAP.left}${CONTROL_MAP.right} 移动 ${CONTROL_MAP.jump} 跳 ${CONTROL_MAP.down} 蹲 ${CONTROL_MAP.attack} 攻 ${CONTROL_MAP.dash} 冲刺(冲刺+攻=重击) | H 显示判定框`, {
      fontSize: '16px',
      color: '#b2bec3',
    });

    this.debugGfx = this.add.graphics();

    this.input.keyboard!.on('keydown-H', () => {
      this.showDebug = !this.showDebug;
      this.debugGfx.clear();
    });

    gameEvents.on('fighter:ko', (payload) => {
      if (this.ended) return;
      this.endMatch(payload.winnerId, 'KO');
    });

    this.refreshHud();
  }

  update(_time: number, delta: number): void {
    if (this.ended) return;

    this.timeLeft -= delta / 1000;
    if (this.timeLeft <= 0) {
      this.timeLeft = 0;
      this.resolveTimeOver();
      return;
    }

    const p1Input = this.inputSystem.poll(this.p1.isDashing);
    const p2Input = this.ai.tick(this.p2, this.p1);

    this.p1.updateLogic(p1Input, this.p2.sprite.x);
    this.p2.updateLogic(p2Input, this.p1.sprite.x);

    CombatResolver.resolve(this.p1, this.p2);

    this.refreshHud();

    this.debugGfx.clear();
    if (this.showDebug) {
      this.p1.drawDebug(this.debugGfx);
      this.p2.drawDebug(this.debugGfx);
    }
  }

  private refreshHud(): void {
    this.drawBar(this.hpBarP1, 120, 48, this.p1.hp, this.p1.maxHp, 0x0984e3);
    this.drawBar(this.hpBarP2, GAME_WIDTH - 120 - 320, 48, this.p2.hp, this.p2.maxHp, 0xd63031);
    this.timerText.setText(String(Math.ceil(this.timeLeft)));
  }

  private drawBar(
    g: Phaser.GameObjects.Graphics,
    x: number,
    y: number,
    hp: number,
    max: number,
    color: number,
  ): void {
    const w = 320;
    const h = 24;
    g.clear();
    g.fillStyle(0x000000, 0.5);
    g.fillRect(x, y, w, h);
    g.fillStyle(color, 1);
    g.fillRect(x, y, w * (hp / max), h);
  }

  private resolveTimeOver(): void {
    if (this.p1.hp === this.p2.hp) {
      this.endMatch('draw', '时间到');
      return;
    }
    const winner = this.p1.hp > this.p2.hp ? 'p1' : 'p2';
    this.endMatch(winner, '时间到');
  }

  private endMatch(winner: 'p1' | 'p2' | 'draw', reason: string): void {
    this.ended = true;
    this.time.delayedCall(800, () => {
      this.scene.start('Result', { winner, reason });
    });
  }
}
