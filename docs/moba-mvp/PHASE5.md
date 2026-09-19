# Phase 5 — 手感与验收

MVP 收官：技能指示、锁定高亮、受击反馈、占位音效、对线节奏调参。

## 手感

| 功能 | 说明 |
|------|------|
| 技能指示器 | 按住 `Q/R/E`（或 1/2/3）显示圆/锥范围 |
| 锁定框 | 普攻范围内最近敌人脚下红环 |
| 受击 | 飘字伤害 + 短暂闪白 |
| 音效 | 程序化短 beep（无外部音频资源） |
| 相机 | 跟随本地英雄 |
| 计时 | HUD 右上角对局时钟 |
| 房间码 | Lobby 提示 Port 作为房间码（默认 7777） |

## 数值（第一轮）

- 塔血 **1800**、水晶 **2400**（便于 8–15 分钟窗口内形成高地压力）
- 兵线间隔 **22s**，小兵攻击略提高
- `BalanceRules` 单测校验无对抗推塔节奏合理

## 真机联调清单

1. 两台设备同一 Wi‑Fi  
2. Host 开房（记下 IP + Port）  
3. Client 填 IP/Port Join  
4. 选英雄 → Start Match → 完整推水晶  
5. 勾选验收：[`acceptance-checklist.md`](./acceptance-checklist.md)

## 本机

**MobaMVP → Setup Phase 0–5 Scenes And Prefabs** 后 Play。
