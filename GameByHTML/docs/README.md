# GameByHTML 文档索引

## 项目愿景

GameByHTML 是一款在**桌面浏览器**中即可游玩的 2D 横版格斗游戏。玩家通过键盘操作角色移动、跳跃、冲刺与攻击，在单关卡内与 CPU 对战直至一方 KO。无需安装客户端，构建产物为静态站点，可部署到任意 CDN 或 GitHub Pages。

## MVP 范围

- 1 名玩家 vs 简单 CPU
- 单战斗场景、双血条 HUD、KO 胜负判定
- 占位美术（色块精灵），重点验证操作与战斗规则
- **不包含**：本地双人对战、在线联机（见 [roadmap.md](./roadmap.md)）

## 如何阅读文档

| 文档 | 内容 |
|------|------|
| [tech-stack.md](./tech-stack.md) | 技术选型、依赖版本、备选方案 |
| [architecture.md](./architecture.md) | 模块分层、固定步长循环、与 Phaser 的边界 |
| [combat-design.md](./combat-design.md) | 键位、Dash+J 重击、伤害与帧数据（与代码 config 对齐） |
| [project-structure.md](./project-structure.md) | 目录树与命名约定 |
| [roadmap.md](./roadmap.md) | 分阶段里程碑与后续功能 |

## 快速开始（代码）

```bash
cd GameByHTML
npm install
npm run dev
```

浏览器打开终端提示的本地地址，默认逻辑分辨率 1280×720。

## 操作说明（玩家）

| 按键 | 功能 |
|------|------|
| A / D | 左 / 右移动 |
| K | 跳跃 |
| S | 蹲伏 |
| W | 预留（MVP 无动作） |
| J | 攻击（冲刺中为重击） |
| L | 冲刺 |

详细战斗规则见 [combat-design.md](./combat-design.md)。
