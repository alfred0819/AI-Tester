# 路线图

## Phase 0 — 文档与工程骨架

- [x] `docs/` 六份规划文档
- [x] Vite + TypeScript + Phaser 初始化
- [x] Boot 场景可启动

**验收**：`npm run dev` 能进入战斗场景框架

## Phase 1 — 移动与跳跃

- [x] WASD（A/D 移动，S 蹲，W 预留）+ K 跳跃
- [x] 地面与重力
- [x] 占位精灵

**验收**：P1 可在舞台内自由移动跳跃

## Phase 2 — 战斗核心

- [x] J 轻击、L Dash
- [x] Dash + J 重击，伤害 ×2
- [x] Hitbox / Hurtbox 与 HUD 扣血

**验收**：打中 CPU 时轻击 10、重击 20

## Phase 3 — 流程与 AI

- [x] Hitstun、击退、KO
- [x] 简单 CPU（靠近、随机攻击/冲刺）
- [x] Result 场景、回合倒计时

**验收**：能完整打完一局

## Phase 4 — 体验打磨（未开始）

- 精灵动画、打击音效、屏幕震动
- 连击保护、防御（W）
- 调试面板：显示 hitbox、帧计数
- Vitest 覆盖 `CombatResolver`、重击判定

## Phase 5 — 本地双人对战（未开始）

- P2 独立键位（如方向键 + 数字键）
- 同键盘冲突检测与提示

## Phase 6 — 在线联机（未开始，仅规划）

- 输入抽象为 `InputSource`
- 回滚或延迟补偿选型评估
- 需后端或 WebRTC 信令，超出 MVP 范围

## 风险跟踪

| 风险 | 对策 |
|------|------|
| 手感偏差 | 调帧表与缓冲长度，开 hitbox 可视化 |
| W/S 与跳跃重复 | MVP 已定为 S 蹲、K 跳 |
| 双人对战键位 | Phase 5 再引入第二套映射 |
