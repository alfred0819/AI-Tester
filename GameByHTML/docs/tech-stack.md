# 技术选型

## 推荐栈总览

| 层级 | 选型 | 版本建议 | 理由 |
|------|------|----------|------|
| 语言 | TypeScript | 5.x | 状态机、判定框、帧数据需要强类型 |
| 构建 | Vite | 6.x | 快速 HMR、静态 `dist` 易部署 |
| 游戏框架 | Phaser | 3.80+ | 场景、输入、相机、精灵管线成熟 |
| 单元测试 | Vitest | 2.x | 与 Vite 同栈，测伤害/重击逻辑 |
| 代码风格 | ESLint + Prettier | 可选 | 团队扩展时使用 |

## 依赖示例（package.json）

```json
{
  "dependencies": {
    "phaser": "^3.80.1"
  },
  "devDependencies": {
    "typescript": "^5.6.0",
    "vite": "^6.0.0",
    "vitest": "^2.1.0"
  }
}
```

## 为何选 Phaser 3

- **场景生命周期**（Boot → Fight → Result）与资源加载清晰
- **Keyboard Plugin** 便于 InputSystem 封装
- **Arcade Physics** 仅用于重力与地面碰撞；**战斗 hitbox 自研**，避免物理引擎不可控的碰撞结果

## 备选方案对比

| 方案 | 优点 | 缺点 | 结论 |
|------|------|------|------|
| 纯 Canvas 2D + 自写循环 | 完全控制 | 动画/资源管线成本高 | 适合后期极致优化，不适合 MVP |
| PixiJS + 自研 | 渲染性能好 | 无内置场景/输入，需自建 | 工作量大 |
| Kaboom.js | 上手快 | 格斗帧数据与状态机表达弱 | 不推荐 |
| Three.js | 3D 生态强 | 2D 格斗非主场景 | 不选 |
| Unity WebGL | 编辑器完整 | 包体大、加载慢 | 不符合「网页即开即玩」 |
| Matter.js 全权 | 物理真实 | 格斗需逐帧判定，难调手感 | 仅环境碰撞可选 |

## 运行环境

- Chrome / Edge / Firefox 近期两个大版本
- 建议窗口宽度 ≥ 1280px；Canvas 按容器 `scale` 适配

## 性能目标

- 渲染与逻辑目标 **60 FPS**
- 逻辑 **固定步长** 1/60s，渲染可插值（MVP 中逻辑与 Phaser update 同步即可）
- 输入缓冲 4 帧（约 66ms @60Hz）

## 部署

```bash
npm run build
# 将 dist/ 上传至静态托管
```

无后端、无 WebSocket 时 MVP 零运维成本。
