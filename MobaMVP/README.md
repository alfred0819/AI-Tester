# MobaMVP — Phase 0+1

原创局域网 MOBA（Unity）。

- **Phase 0**：Boot → Lobby → Arena、NGO Host/Client、WASD 移动
- **Phase 1**：普攻、冲击波技能、Host 权威伤害、死亡复活、训练假人

## 环境要求

- Unity **6000.0 LTS**（或相近 Unity 6）

## 首次打开

1. Unity Hub → **Add** → `MobaMVP/`
2. 等待包导入完成
3. 菜单 **MobaMVP → Setup Phase 0+1 Scenes And Prefabs**
4. 打开 `Assets/_Project/Scenes/Boot.unity`，Play

## 操作

| 按键 | 动作 |
|------|------|
| WASD | 移动 |
| J / 鼠标左键 | 普攻 |
| K / Q | 冲击波技能 |

## 局域网

1. 实例 A：Host → Start Match
2. 实例 B：Join `127.0.0.1`
3. 互殴或打红色 TrainingDummy

## Core 单测

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test MobaMVP/src/Moba.Core.Tests/Moba.Core.Tests.csproj
```

## 下一阶段（Phase 2）

兵线、防御塔攻击、水晶胜负结算、补刀金币。
