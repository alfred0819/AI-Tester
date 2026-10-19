# 架构与帧数据协议

## 1. 仿真循环（60 Hz）

每逻辑帧顺序：

1. 采样并推入 `InputBuffer`（含方向历史）
2. 消耗 `hitstop`（双方冻结逻辑位移/动画帧，仍可记录输入）
3. 解析指令 → 请求状态转移（受 CancelRules / 硬直约束）
4. 推进当前 `MoveDef` 帧；更新 Hurt/Hit boxes
5. 重力与位移积分；边界夹紧；角色间推挤
6. `AttackResolver`：HitBox vs 对方 HurtBox；写入命中事件
7. 应用伤害、气槽、硬直、击退；切换 HitStun / BlockStun
8. 检查 KO → 通知 `MatchController` 换人/胜负
9. 写出 `FighterSnapshot` 供表现层

Training 模式可在步骤后附加：帧数 HUD、一键复位、锁血锁气。

## 2. 输入协议

每帧采样为：

```text
dir: int8          # numpad 1..9（5=中立）
buttons: bitset    # LP MP HP LK MK HK + Start
pressed: bitset    # 本帧新按下
released: bitset
```

`InputBuffer` 保留最近 **12～15 帧** 方向与按键，用于：

- 波动拳类：`236 + P`
- 升龙类：`623 + P`
- 投：近身 `6+P+K`（或简化为近身双键）

缓冲窗口按招式配置（默认 8 帧）。

## 3. MoveDef（招式定义）

JSON / Resource 字段：

```text
move_id: string
command: string              # "5A" | "236A" | "623C" | "214214D" ...
startup: int
active: int
recovery: int
damage: int
chip_damage: int             # 防住时
hitstun: int
blockstun: int
pushback_hit: float
pushback_block: float
hitstop_attacker: int
hitstop_defender: int
height: Mid | High | Low | Overhead
attribute: Strike | Throw | Projectile
meter_gain_hit: float
meter_cost: float            # 超必杀等
cancel_windows: [{from_frame, to_frame, into_tags[]}]
hitboxes: [{from, to, x, y, w, h, dmg_override?}]
hurtbox_overrides: [...]     # 可选，覆盖常态受击框
flags: {launch, knockdown, guard_crush, armor_break, invuln_frames}
```

普攻用短 id（`5A`/`2B`/`j.C`）；必杀用指令串。

## 4. CharacterDef

```text
char_id
display_name
max_hp
walk_speed, back_walk_speed, dash_speed
jump_velocity, gravity
idle_hurtboxes[]
moves[]: MoveDef refs
portrait, color_theme          # Q版 UI
```

## 5. 命中裁定

优先级建议：

1. 投技与打击同一帧互斥规则（近身投优先或按双方状态）
2. 双方同时攻击 → Clash / 对撞（MVP 可简化为双方都挨打或按伤害高者）
3. 已处于 HitStun 再被打 → 连段；应用 juggle 衰减
4. 防御中：低段对站防 / 上段对蹲防惩罚；余下进入 BlockStun + 削气或削血

Guard Crush：防御条或「连续防压值」超过阈值 → 破防硬直（可用简化条）。

## 6. 气槽规则（第一版）

```text
meter: 0..300     # 显示为 3 格
gain_on_hit = move.meter_gain_hit
gain_on_block_defend = small
cost_super = 100 或 300（按招）
```

MAX 模式若做：满 3 格可激活，限时伤害加成 + 可取消范围扩大；**可放到 Phase 5 可选**。

## 7. 表现层桥接

`FighterView` 订阅 snapshot：

- 动画名 = `state` + `move_id`
- hitstop 时暂停 AnimatedSprite
- 生成 hit spark / 防守火花 / 超级特写（Q 版夸张缩放）

禁止在 View 内改 HP/气槽。

## 8. 模块边界

| 目录 | 职责 | 依赖 |
|------|------|------|
| `scripts/core/` | 输入缓冲、状态、框体、伤害、气槽 | 无 Node 亦可跑测 |
| `scripts/match/` | 队序、回合、Arcade 进度 | core |
| `scripts/ai/` | CPU 决策 → 伪造输入 | core |
| `scripts/presentation/` | 精灵、VFX、相机 | core 快照 |
| `scripts/ui/` | 选人、HUD、暂停 | match 信号 |

## 9. 与 MobaMVP 的差异（避免混用假设）

- 无 Host 网络权威；本地定票即可
- 无装备经济；成长全靠气槽与换人
- 判定是帧级框体，不是技能形状 AOE
