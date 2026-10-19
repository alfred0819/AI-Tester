import Phaser from 'phaser';
import { DASH, getMove, type MoveDef } from '../config/combat';
import { PHYSICS, STAGE } from '../config/game';
import { gameEvents } from '../core/EventBus';
import { hurtbox, rectsOverlap, worldHitbox, type Rect } from '../combat/Hitbox';
import type { FrameInput } from '../input/InputSystem';

export type FighterId = 'p1' | 'p2';

export type FighterState =
  | 'idle'
  | 'walk'
  | 'jump'
  | 'crouch'
  | 'dash'
  | 'attackLight'
  | 'attackHeavy'
  | 'hitstun'
  | 'dead';

const BODY_W = 48;
const BODY_H = 80;

export class Fighter {
  sprite: Phaser.Physics.Arcade.Sprite;
  state: FighterState = 'idle';
  facing: 1 | -1 = 1;
  hp: number;
  readonly maxHp: number;

  private dashFramesLeft = 0;
  private dashCooldown = 0;
  private attackFrame = 0;
  private currentMove: MoveDef | null = null;
  private hitConfirmed = false;
  private hitstunFrames = 0;
  private knockbackFrames = 0;
  private knockbackSpeed = 0;
  private crouching = false;

  constructor(
    scene: Phaser.Scene,
    public readonly id: FighterId,
    x: number,
    color: number,
    maxHp: number,
  ) {
    this.maxHp = maxHp;
    this.hp = maxHp;

    const texKey = `fighter_${id}`;
    if (!scene.textures.exists(texKey)) {
      const g = scene.make.graphics({ x: 0, y: 0 }, false);
      g.fillStyle(color, 1);
      g.fillRect(0, 0, BODY_W, BODY_H);
      g.generateTexture(texKey, BODY_W, BODY_H);
      g.destroy();
    }

    this.sprite = scene.physics.add.sprite(x, STAGE.floorY, texKey);
    this.sprite.setOrigin(0.5, 1);
    this.sprite.setCollideWorldBounds(false);
    this.sprite.body!.setSize(BODY_W - 8, BODY_H - 4);
    this.sprite.body!.setOffset(4, 4);
    if (id === 'p2') this.facing = -1;
    this.syncFlip();
  }

  get isDashing(): boolean {
    return this.state === 'dash' && this.dashFramesLeft > 0;
  }

  get isDead(): boolean {
    return this.state === 'dead';
  }

  updateLogic(input: FrameInput, opponentX: number): void {
    if (this.state === 'dead') return;

    if (this.dashCooldown > 0) this.dashCooldown--;

    if (this.hitstunFrames > 0) {
      this.tickKnockback();
      this.hitstunFrames--;
      if (this.hitstunFrames <= 0) {
        this.state = 'idle';
        this.sprite.setVelocityX(0);
      }
      this.clampStage();
      return;
    }

    if (this.state === 'attackLight' || this.state === 'attackHeavy') {
      this.tickAttack();
      this.clampStage();
      return;
    }

    if (this.state === 'dash') {
      this.dashFramesLeft--;
      this.sprite.setVelocityX(this.facing * DASH.speed * 60);
      this.sprite.setVelocityY(0);
      if (input.attackPressed || input.attack) {
        this.startAttack(true);
        this.clampStage();
        return;
      }
      if (this.dashFramesLeft <= 0) {
        this.state = 'idle';
        this.dashCooldown = DASH.cooldownFrames;
        this.sprite.setVelocityX(0);
      }
      this.clampStage();
      return;
    }

    this.updateFacing(opponentX);

    const onGround = this.sprite.body!.blocked.down || this.sprite.y >= STAGE.floorY - 1;

    if (input.dashPressed && onGround && this.dashCooldown <= 0) {
      this.startDash();
      this.clampStage();
      return;
    }

    if (input.attackPressed) {
      this.startAttack(this.isDashing);
      this.clampStage();
      return;
    }

    if (input.jumpPressed && onGround && !this.crouching) {
      this.state = 'jump';
      this.sprite.setVelocityY(PHYSICS.jumpVelocity);
    }

    this.crouching = onGround && input.down;
    if (this.crouching) {
      this.state = 'crouch';
      this.sprite.setVelocityX(0);
    } else if (!onGround) {
      this.state = 'jump';
      const factor = PHYSICS.airWalkFactor;
      let vx = 0;
      if (input.left) vx -= PHYSICS.walkSpeed * 60 * factor;
      if (input.right) vx += PHYSICS.walkSpeed * 60 * factor;
      this.sprite.setVelocityX(vx);
    } else {
      let vx = 0;
      if (input.left) vx -= PHYSICS.walkSpeed * 60;
      if (input.right) vx += PHYSICS.walkSpeed * 60;
      this.sprite.setVelocityX(vx);
      this.state = vx !== 0 ? 'walk' : 'idle';
    }

    if (onGround && this.sprite.body!.velocity.y > 0) {
      this.sprite.setVelocityY(0);
      this.sprite.y = STAGE.floorY;
    }

    this.clampStage();
  }

  private startDash(): void {
    this.state = 'dash';
    this.dashFramesLeft = DASH.durationFrames;
    this.crouching = false;
  }

  private startAttack(heavy: boolean): void {
    if (this.state === 'dash') {
      heavy = true;
      this.dashFramesLeft = 0;
      this.dashCooldown = DASH.cooldownFrames;
    }
    this.state = heavy ? 'attackHeavy' : 'attackLight';
    this.currentMove = getMove(heavy);
    this.attackFrame = 0;
    this.hitConfirmed = false;
    this.sprite.setVelocityX(0);
  }

  private tickAttack(): void {
    if (!this.currentMove) return;
    this.attackFrame++;
    const total = this.currentMove.startup + this.currentMove.active + this.currentMove.recovery;
    if (this.attackFrame >= total) {
      this.state = 'idle';
      this.currentMove = null;
      this.sprite.setVelocityX(0);
    }
  }

  private tickKnockback(): void {
    if (this.knockbackFrames > 0) {
      this.sprite.setVelocityX(this.facing * -this.knockbackSpeed * 60);
      this.knockbackFrames--;
    }
  }

  canDealHit(): boolean {
    if (!this.currentMove || this.hitConfirmed) return false;
    const { startup, active } = this.currentMove;
    const f = this.attackFrame;
    return f > startup && f <= startup + active;
  }

  getActiveHitbox(): Rect | null {
    if (!this.canDealHit() || !this.currentMove) return null;
    return worldHitbox(
      this.sprite.x,
      this.sprite.y,
      this.facing,
      this.currentMove.hitbox,
    );
  }

  overlapsHit(target: Rect): boolean {
    const hit = this.getActiveHitbox();
    return hit ? rectsOverlap(hit, target) : false;
  }

  getHurtbox(): Rect {
    return hurtbox(this.sprite.x, this.sprite.y, BODY_W, BODY_H, this.crouching);
  }

  receiveHit(attacker: Fighter): void {
    if (this.state === 'dead' || this.hitstunFrames > 0) return;
    if (!attacker.canDealHit() || !attacker.currentMove) return;
    if (attacker.hitConfirmed) return;

    attacker.hitConfirmed = true;
    const move = attacker.currentMove;
    const heavy = attacker.state === 'attackHeavy';

    this.hp = Math.max(0, this.hp - move.damage);
    this.hitstunFrames = move.hitstun;
    this.knockbackFrames = move.knockbackFrames;
    this.knockbackSpeed = move.knockbackSpeed;
    this.facing = attacker.facing;
    this.state = 'hitstun';
    this.currentMove = null;

    gameEvents.emit('fighter:hit', {
      attackerId: attacker.id,
      damage: move.damage,
      heavy,
    });

    if (this.hp <= 0) {
      this.state = 'dead';
      this.sprite.setVelocity(0, 0);
      gameEvents.emit('fighter:ko', {
        winnerId: attacker.id,
        reason: 'ko',
      });
    }
  }

  private updateFacing(opponentX: number): void {
    if (this.state === 'attackLight' || this.state === 'attackHeavy') return;
    this.facing = opponentX >= this.sprite.x ? 1 : -1;
    this.syncFlip();
  }

  private syncFlip(): void {
    this.sprite.setFlipX(this.facing === -1);
  }

  private clampStage(): void {
    this.sprite.x = Phaser.Math.Clamp(this.sprite.x, STAGE.leftBound, STAGE.rightBound);
  }

  drawDebug(graphics: Phaser.GameObjects.Graphics): void {
    const hurt = this.getHurtbox();
    graphics.fillStyle(this.id === 'p1' ? 0x00ff0044 : 0xff000044, 0.3);
    graphics.fillRect(hurt.x, hurt.y, hurt.w, hurt.h);
    const hit = this.getActiveHitbox();
    if (hit) {
      graphics.fillStyle(0xffff0044, 0.5);
      graphics.fillRect(hit.x, hit.y, hit.w, hit.h);
    }
  }
}
