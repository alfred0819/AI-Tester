# KofQMvp — Q 版小队格斗（Godot 4）

玩法对齐拳皇 2002 品类（3 人队、气槽、取消、超必杀），角色与美术为**原创 Q 版**。  
规划文档：[`docs/kof-q-mvp/`](../docs/kof-q-mvp/INDEX.md)

## 当前进度

| Phase | 状态 |
|-------|------|
| 0 工程骨架 | 进行中（仓库已含 `project.godot` + 场景/脚本占位） |
| 1～5 | 见规划文档 |

## 本机运行

1. 安装 **Godot 4.3+**
2. 导入 / 打开本目录 `KofQMvp/`
3. 运行主场景（Boot）→ Title → 可进 Fight 灰盒

### 默认键位（Phase 0）

| 动作 | 1P | 2P |
|------|----|----|
| 移动 | WASD | 方向键 |
| 轻拳 / 轻脚 | J / K | Num0 / Num. |
| 中拳 / 中脚 | U / I | （后续补键） |

## 目录

- `scenes/` 场景流
- `scripts/core/` 定票战斗逻辑
- `scripts/match/` 队序与模式
- `scripts/presentation/` 表现
- `data/` 角色与招式表

## 版权

禁止使用 SNK / 拳皇商业素材与角色名。
