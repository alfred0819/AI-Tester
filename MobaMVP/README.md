# MobaMVP — Phase 0–6

原创局域网 MOBA。MVP（0–5）可玩闭环 + Phase 6 结算/小地图/回城/断线。

## 进度

| Phase | 内容 |
|-------|------|
| 0–2 | LAN、战斗、推塔胜负 |
| 3 | 四英雄 + 商店 |
| 4 | 3v3 + Fill Bots |
| 5 | 技能指示、锁定、受击反馈、音效、节奏调参 |
| 6 | 结算 KDA、小地图、T 回城、断线策略 |

## 本机

1. Unity 打开 `MobaMVP/`
2. **MobaMVP → Setup Phase 0–6 Scenes And Prefabs**
3. Lobby 选模式/英雄 → Host → Start Match
4. `T` 回城；拆水晶看战绩面板；左下角小地图

## 真机

同 Wi‑Fi：Client 填 Host IP，Port 默认 `7777`。验收见 `docs/moba-mvp/acceptance-checklist.md`。

## 单测

```bash
dotnet test MobaMVP/src/Moba.Core.Tests/Moba.Core.Tests.csproj
```
