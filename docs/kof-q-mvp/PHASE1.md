# Phase 1 — 灰盒互殴闭环

## 目标

单人生命值下，双方可用普攻对打、防御、受击硬直；一方 HP≤0 宣布本局胜利（尚未换人）。

## 任务清单

- [ ] `HurtBox` / `HitBox` AABB 与调试绘制（Training 可开关）
- [ ] 至少一套站立轻/中攻击（可共用动画）
- [ ] 命中：伤害、hitstun、pushback、hitstop
- [ ] 按住后方向进入 Block；blockstun + 少量 chip（可先 0）
- [ ] HP 条 HUD
- [ ] KO 后冻结输入，显示 Winner，按键回选人

## 验收

1. 1P 连打 2P 可打死；防守可挡住中段
2. 命中时有可见停顿与击退
3. 调试模式能看到红 Hit / 绿 Hurt 框

## 下一阶段

Phase 2：指令必杀、气槽、超必杀、取消、紧急回避。
