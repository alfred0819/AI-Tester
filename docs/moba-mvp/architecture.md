# 架构与数据协议草稿

## 1. 仿真循环

Host 固定步长 `dt = 0.05s`（20 Hz）：

1. 收集本帧玩家 `PlayerCommand`
2. 应用移动、技能施放请求
3. 更新弹道、DoT、状态效果
4. 更新小兵 AI、防御塔 AI、Bot
5. 结算伤害与死亡
6. 检查水晶 → 触发结束
7. 写出需同步的 NetworkVariable / 脏事件

客户端：采样输入 → 发送 Command → 本地表现预测（移动/技能前摇）→ 接收权威状态校正。

## 2. PlayerCommand（客户端 → Host）

每 Tick 或按变更发送（MVP 可用不可靠 + 序列号）：

```text
seq: uint32
moveX, moveY: float   // 归一化，平面
attackPressed: bool
skillSlot: byte       // 0=无, 1..4
aimX, aimY: float     // 技能方向或落点（相对自己或世界坐标需约定）
targetNetId: uint32   // 锁定目标，0=无
shopBuyItemId: uint16 // 0=无购买
```

Host 校验：

- 技能 CD / 存活 / 控制状态
- 购买时金币与背包格
- aim 距离截断在技能 MaxRange

## 3. 同步状态（Host → Client）

优先 NetworkVariable：

- 位置、朝向、HP、MaxHP
- 阵营、英雄 Id、存活、复活剩余
- 金币、装备栏 6 格
- 塔/水晶 HP

事件型（RPC）：

- 技能释放开始（slot、aim、target）用于播动画
- 击杀播报、推塔、胜利

## 4. 伤害公式（第一版）

物理：

```text
raw = atk * skillRatio
reduced = raw * (1 - armor / (armor + 100))
```

法术：同理换 `ap` 与 `mr`。

暴击、穿透、全能吸血：MVP 可后置；先做穿透百分比一档即可。

## 5. 技能定义 JSON/SO 字段

```text
skillId
castType: Self | Direction | Point | Unit
shape: None | Circle | Cone | Box | Projectile
range, radius, angle, boxSize
castTime, recoverTime, cooldown
costs[]                     // 可选
effects[]:
  type: Damage | Heal | Stun | Slow | Dash | Shield | Spawn
  amount / duration / speedMul / ...
  targetFilter: Enemy | Ally | Self
```

## 6. 场景对象约定

- 出生点：`SpawnPoint_Team{0|1}_{index}`
- 兵线路径：`LanePath_{top|mid|bot}_Team{0|1}`（Waypoint 列表）
- 塔：`Tower_Team{0|1}_Tier1`
- 水晶：`Crystal_Team{0|1}`

单路图只用 `mid`。

## 7. 程序集边界

- `Moba.Core`：属性、伤害、技能解析、胜负、经济（可 NUnit 测试）
- `Moba.Unity`：MonoBehaviour、NGO、动画、UI、寻路

Core 不引用 UnityEngine（或仅 `#if` 隔离），便于 CI 无编辑器跑测。
