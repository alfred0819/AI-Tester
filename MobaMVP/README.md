# MobaMVP — Phase 0 工程骨架

原创局域网 MOBA（Unity）。当前完成 **Phase 0**：Boot → Lobby → Arena、NGO Host/Client、灰盒玩家生成与 WASD 移动。

## 环境要求

- Unity **6000.0 LTS**（或相近 Unity 6；`ProjectSettings/ProjectVersion.txt` 可按本机版本改）
- 模块：Windows/Mac/Linux Build Support（按需）

## 首次打开

1. Unity Hub → **Add** → 选择本目录 `MobaMVP/`
2. 等待 URP / Input System / Netcode 包导入完成
3. 菜单 **MobaMVP → Setup Phase 0 Scenes And Prefabs**
4. 打开 `Assets/_Project/Scenes/Boot.unity`，点击 Play

## 局域网试玩

1. **实例 A**：Lobby → Host → Start Match  
2. **实例 B**：Lobby 填入 Host IP（本机第二窗口用 `127.0.0.1`）→ Join  
3. Arena 中用 **WASD** 移动；蓝/红胶囊为阵营色

可用：ParrelSync / Multiplayer Play Mode / 一次 Editor + 一次 Build。

## 目录

```
Assets/_Project/
  Editor/Phase0SetupMenu.cs     # 一键生成场景与 Prefab
  Scripts/Core/                 # 无引擎依赖逻辑（可单测）
  Scripts/Network/              # NGO 封装、玩家 Avatar
  Scripts/Bootstrap/            # Boot / Session / Arena
  Scripts/UI/LobbyMenu.cs
  Scenes/                       # 运行 Setup 后生成
  Prefabs/
```

## 命令行 Core 单测（无需 Unity）

在仓库根目录：

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test MobaMVP/src/Moba.Core.Tests/Moba.Core.Tests.csproj
```

## 下一阶段（Phase 1）

- 普攻锁定 + 一个技能
- Host 权威伤害与死亡复活
- 1v1 空场互殴
