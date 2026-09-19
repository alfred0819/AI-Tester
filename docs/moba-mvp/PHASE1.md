# Phase 1 — 单英雄灰盒对战

## 目标

移动 + 普攻 + 冲击波技能 + Host 权威伤害 + 死亡 5 秒复活；1v1 或打训练假人可玩。

## 操作

| 按键 | 动作 |
|------|------|
| WASD | 移动 |
| J / 鼠标左键 | 普攻（锁定范围内最近敌方） |
| K / Q | 冲击波（自身周围圆范围魔法伤害） |

## 内容

- `HeroCombatState` / `Phase1HeroKit`：CD、伤害、复活（Core 单测）
- `HeroCombat`：NetworkVariable 同步 HP/存活/CD
- `PlayerCombatInput`：战斗输入
- `TrainingDummy`：红方固定阵营假人（Arena 自动生成）
- 头顶血条 + 战斗 HUD

## 本机步骤

1. Unity 打开 `MobaMVP/`
2. **MobaMVP → Setup Phase 0+1 Scenes And Prefabs**（若已跑过 Phase0，请再跑一次以刷新 Prefab）
3. Play Boot → Host → Start Match
4. 走近红色假人，J 普攻 / K 技能；或第二客户端 Join 互殴

## 验收

- [ ] 普攻掉血，有攻击间隔
- [ ] 技能有范围特效与 CD
- [ ] HP≤0 变矮，约 5 秒后泉水满状态复活
- [ ] 两名玩家可互相造成伤害
