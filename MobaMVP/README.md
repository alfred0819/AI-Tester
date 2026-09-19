# MobaMVP — Phase 0–2

原创局域网 MOBA（Unity）。当前完成到 **推塔胜负闭环**。

## 进度

| Phase | 内容 |
|-------|------|
| 0 | Boot/Lobby/Arena、LAN Host/Client、WASD |
| 1 | 普攻、冲击波、伤害、复活、假人 |
| 2 | 兵线、塔、水晶胜负、补刀金币 |

## 本机

1. Unity Hub 打开 `MobaMVP/`
2. **MobaMVP → Setup Phase 0+1+2 Scenes And Prefabs**
3. Play `Boot` → Host → Start Match
4. 跟线补刀 → 拆塔 → 拆水晶获胜

## 操作

| 按键 | 动作 |
|------|------|
| WASD | 移动 |
| J / 鼠标左键 | 普攻 |
| K / Q | 冲击波 |

## 单测

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test MobaMVP/src/Moba.Core.Tests/Moba.Core.Tests.csproj
```

## 下一阶段（Phase 3）

四英雄技能数据化 + 六件装备商店。
