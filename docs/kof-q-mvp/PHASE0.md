# Phase 0 — 工程骨架

## 目标

可运行的 Godot 4 工程：标题 → 选人（占位）→ 对战场；两名灰盒角色能左右移动、跳跃、落地，相机跟随，左右边界生效。

## 任务清单

- [x] 创建 `KofQMvp/project.godot`（2D、主场景 Boot）
- [x] InputMap：`p1_*` / `p2_*` 方向与 4 键（轻中拳脚；重击 Phase 1+）
- [x] 场景：`boot` → `title` → `fight`（`char_select` 留待 Phase 3）
- [x] `FightSimulator` 以 60 Hz `_physics_process` 推进
- [x] 灰盒 `FighterBody`：矩形色块、走跳、重力、地面与边界
- [x] 相机跟随两人中点并夹紧
- [ ] 本机 Godot 编辑器打开做一次手操验收（环境未预装 Godot 时跳过，开发机补验）

## 验收

1. F5 从 Boot 进入 Fight，1P/2P 可同时走动跳跃
2. 角色不会掉出世界；相机不抖出舞台
3. 工程目录符合 [README.md](README.md) 第 6 节

## 产出路径

[`KofQMvp/`](../../KofQMvp/README.md) 初始工程已入库；框体重叠逻辑有 `KofQMvp/tests/test_aabb2.py`。

## 下一阶段

Phase 1：攻击框、普攻、防御、伤害与单人 KO。
