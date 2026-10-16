# Phase 1 — 灰盒互殴闭环

## 状态：完成

## 目标

单人生命值下，双方可用普攻对打、防御、受击硬直；一方 HP≤0 宣布本局胜利（尚未换人）。

## 任务清单

- [x] `HurtBox` / `HitBox` AABB 与调试绘制（Training 可开关 `F`）
- [x] 至少一套站立轻/中攻击（5A/5B/5C/2A）
- [x] 命中：伤害、hitstun、pushback、hitstop
- [x] 按住后方向进入 Block；blockstun + chip
- [x] HP 条 HUD
- [x] KO 后冻结输入，显示 Winner（Phase 3 起换人）

## 验收

1. 1P 连打 2P 可打死；防守可挡住中段 — 自动化 + 本机
2. 命中时有可见停顿与击退
3. 调试模式能看到红 Hit / 绿 Hurt 框

## 下一阶段

Phase 2：指令必杀、气槽、超必杀、取消、紧急回避。
