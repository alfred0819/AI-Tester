# 战斗设计

本文档与代码 **`src/config/controls.ts`**、**`src/config/combat.ts`** 保持一致；改规则时先改 config，再同步本文档。

## 键位表（玩家 P1）

| 按键 | KeyCode | 动作 |
|------|---------|------|
| W | KeyW | 预留，MVP 无效果 |
| A | KeyA | 向左移动 |
| S | KeyS | 蹲伏（地面且非攻击/受击时） |
| D | KeyD | 向右移动 |
| J | KeyJ | 攻击 |
| K | KeyK | 跳跃 |
| L | KeyL | 冲刺（Dash） |

## 移动

- 地面 walkSpeed：`5` px/逻辑帧（@60Hz）
- 空中可左右微调，系数 `0.7`
- 跳跃初速度：`-420`（配合 Arcade 重力 `900`）
- 蹲伏：hurtbox 高度 ×0.55，不可移动直至松开 S

## 冲刺（L）

- 触发：地面、非 Hitstun、非攻击 recovery 中
- 持续 **12 帧**，水平速度 **12** px/帧，方向为当前朝向
- 冷却 **30 帧**（Dash 结束后才能再次 L）
- Dash 期间可按 **J** 发动重击（见下）

## 攻击

### 轻击（J，非 Dash 中）

| 属性 | 值 |
|------|-----|
| startup | 4 帧 |
| active | 3 帧 |
| recovery | 8 帧 |
| 伤害 | 10 |
| hitbox | 宽 48×高 32，位于身前 |

### 重击（Dash 期间 + J，或 Dash 窗口内 4 帧输入缓冲内按 J）

| 属性 | 值 |
|------|-----|
| startup | 6 帧 |
| active | 4 帧 |
| recovery | 12 帧 |
| 伤害 | **20**（= 10 × 2） |
| hitbox | 宽 64×高 40 |

**判定逻辑：**

```
canHeavy = fighter.isDashing || input.consumeBufferedAttackDuringDash()
damage = canHeavy ? baseDamage * HEAVY_MULTIPLIER : baseDamage
```

`HEAVY_MULTIPLIER = 2`（定义于 `combat.ts`）

重击发动时 **立即结束 Dash 位移**（进入 AttackHeavy 状态）。

### 命中规则

- 仅在 **active** 帧检测 hitbox vs 对手 hurtbox
- 同一招式实例最多命中 **1** 次（`hitConfirmed` 标志）
- 对手处于 **Hitstun** 时仍可被连击（MVP 不实现连击保护）

## 受击

- Hitstun：**轻击 12 帧**，**重击 20 帧**
- 击退：轻击 `4` px/帧 × 8 帧，重击 `6` px/帧 × 12 帧（沿攻击方向）
- HP 归零 → `Dead`，广播 `fighter:ko`

## 胜负

- 初始 HP：**100**
- 一方 Dead → 切换 Result 场景
- 可选：99 秒计时（MVP 已实现倒计时，归零判双方 HP 多者胜）

## W/S MVP 定稿

- **S**：蹲伏（方案 A）
- **W**：无动作，预留格挡或超杀

## 帧数据模板（扩展用）

```typescript
interface MoveDef {
  id: string;
  startup: number;
  active: number;
  recovery: number;
  damage: number;
  hitbox: { w: number; h: number; offsetX: number; offsetY: number };
  hitstun: number;
}
```
