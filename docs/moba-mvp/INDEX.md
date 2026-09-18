# 原创 MOBA MVP 规划索引

本仓库当前交付为 **Unity 局域网 1v1/3v3 可玩 MVP** 的完整规划（非引擎工程本体）。

## 文档

| 文档 | 说明 |
|------|------|
| [docs/moba-mvp/README.md](docs/moba-mvp/README.md) | 总规划：范围、技术栈、架构、英雄、分阶段实施 |
| [docs/moba-mvp/architecture.md](docs/moba-mvp/architecture.md) | 仿真循环、Command 协议、伤害与技能数据 |
| [docs/moba-mvp/acceptance-checklist.md](docs/moba-mvp/acceptance-checklist.md) | 「能玩」验收清单 |

## 已锁定决策

- 范围：**MVP 可对战**（1v1 / 3v3，局域网，推塔胜负）
- 引擎：**Unity**（Netcode for GameObjects）
- 内容：原创英雄与灰盒地图，不使用《王者荣耀》商用 IP 资产

## 实现顺序（摘要）

`工程骨架 → 单英雄互殴 → 兵线推塔闭环 → 四英雄+装备 → 3v3+Bot → 真机打磨`
