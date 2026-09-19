# 原创 MOBA MVP 规划索引

**Unity 局域网 1v1/3v3** — 规划 + Phase 0 工程骨架已落地。

## 文档与工程

| 文档 / 目录 | 说明 |
|------|------|
| [README.md](README.md) | 总规划：范围、技术栈、架构、英雄、分阶段实施 |
| [PHASE0.md](PHASE0.md) | Phase 0 完成说明 |
| [architecture.md](architecture.md) | 仿真循环、Command 协议、伤害与技能数据 |
| [acceptance-checklist.md](acceptance-checklist.md) | 「能玩」验收清单 |
| [../../MobaMVP/README.md](../../MobaMVP/README.md) | Unity 工程打开与联调步骤 |

## 已锁定决策

- 范围：**MVP 可对战**（1v1 / 3v3，局域网，推塔胜负）
- 引擎：**Unity**（Netcode for GameObjects）
- 内容：原创英雄与灰盒地图，不使用《王者荣耀》商用 IP 资产

## 实现顺序

`工程骨架(✓ Phase0) → 单英雄互殴 → 兵线推塔闭环 → 四英雄+装备 → 3v3+Bot → 真机打磨`
