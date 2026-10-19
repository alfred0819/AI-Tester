# 项目结构

```
GameByHTML/
├── docs/                      # 规划与设计文档
├── public/
│   └── assets/                # 静态资源（精灵、音效，可空）
├── index.html
├── package.json
├── vite.config.ts
├── tsconfig.json
├── vitest.config.ts           # 可选单元测试
├── README.md
└── src/
    ├── main.ts                # Phaser.Game 入口
    ├── config/
    │   ├── controls.ts        # 键位 KeyCode 映射（单一数据源）
    │   ├── combat.ts          # 伤害、帧数、Dash 参数
    │   └── game.ts            # 分辨率、重力、舞台边界
    ├── core/
    │   └── EventBus.ts
    ├── input/
    │   └── InputSystem.ts
    ├── combat/
    │   ├── Hitbox.ts
    │   └── CombatResolver.ts
    ├── entities/
    │   ├── Fighter.ts
    │   └── AIController.ts
    └── scenes/
        ├── BootScene.ts
        ├── FightScene.ts
        └── ResultScene.ts
```

## 命名约定

- 场景类：`PascalCase` + `Scene` 后缀
- 配置文件：纯数据 + `export const`，无 side effect
- 逻辑帧计数：Fighter 内 `logicFrame` 整数递增，与 Phaser `time` 分离

## 配置扩展

1. **新键位**：修改 `controls.ts` 的 `CONTROL_MAP`，InputSystem 自动读取
2. **新招式**：在 `combat.ts` 增加 `LIGHT_ATTACK` / `HEAVY_ATTACK` 或新 MoveDef
3. **新角色**：Fighter 构造参数 `fighterId`，从 `fighters` 表读 maxHp、配色

## 脚本

| 命令 | 说明 |
|------|------|
| `npm run dev` | 开发服务器 |
| `npm run build` | 生产构建 |
| `npm run preview` | 预览 dist |
| `npm test` | Vitest（伤害/重击公式） |
