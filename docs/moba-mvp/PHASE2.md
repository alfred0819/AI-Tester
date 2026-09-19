# Phase 2 — 推塔闭环

## 目标

单路兵线 + 防御塔 + 水晶胜负 + 补刀金币。

## 玩法

1. 开局约 5 秒后双方出兵，之后约每 25 秒一波（每波 3 近战）
2. 小兵沿中路推进，遇敌交战，可打塔
3. **先拆对方防御塔**，水晶才会解锁（可攻击）
4. 摧毁对方水晶 → 结算面板 → 返回大厅
5. 补刀小兵 / 推塔 / 击杀英雄得金币；另有被动金币

## 操作（同 Phase 1）

WASD 移动 · J/LMB 普攻 · K/Q 冲击波

## 本机

1. Unity 打开 `MobaMVP/`
2. **MobaMVP → Setup Phase 0+1+2 Scenes And Prefabs**（务必重跑）
3. Play Boot → Host → Start Match
4. 跟线补刀 → 拆塔 → 拆水晶

## 新增脚本

- `MinionUnit` / `LaneWaveSpawner`
- `DefenseTower` / `NexusCrystal` / `ArenaStructureSpawner`
- `MatchResultController`
- `ICombatTarget` 统一受击
- Core：`Phase2Rules`（经济 / 建筑 / 胜负）
