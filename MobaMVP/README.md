# MobaMVP — Phase 0–3

原创局域网 MOBA（Unity）。当前完成到 **四英雄 + 商店 + 推塔胜负**。

## 进度

| Phase | 内容 |
|-------|------|
| 0 | LAN Host/Client、场景流 |
| 1 | 普攻/技能/复活 |
| 2 | 兵线、塔、水晶胜负、金币 |
| 3 | 四英雄数据技能、六件装备、眩晕减速 |

## 本机

1. Unity Hub 打开 `MobaMVP/`
2. **MobaMVP → Setup Phase 0–3 Scenes And Prefabs**
3. Lobby 选英雄 → Host → Start Match
4. `Q/R/E` 技能，`B` 商店，推塔拆水晶

## 单测

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test MobaMVP/src/Moba.Core.Tests/Moba.Core.Tests.csproj
```

## 下一阶段（Phase 4）

3v3 与补位 Bot。
