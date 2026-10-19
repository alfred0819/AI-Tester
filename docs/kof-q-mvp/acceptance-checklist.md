# Q 版格斗 MVP 验收清单

勾选即视为对应项通过。

## 工程与模式

- [x] Godot 4 打开 `KofQMvp/` 可直接运行
- [x] Title 可进 Versus / Training / Arcade
- [x] Versus：双方选 3 人顺序后进入对战
- [x] Training：锁血锁气、复位、假人防/站
- [x] Arcade：至少 3 关 CPU，有胜负回退（实现 4 关）

## 战斗系统

- [x] 走、跑或冲、跳、蹲可用（走/跳/蹲；冲刺以回避位移替代）
- [x] 轻/中（或轻中重）普攻可命中与防守
- [x] Hitstop、击退、硬直可感知
- [x] 指令必杀至少每角色 2 个可稳定输入打出
- [x] 气槽涨耗正确；超必杀耗气
- [x] 普攻取消进必杀在训练场可复现
- [x] 紧急回避可躲一次攻击（含无敌帧）
- [x] KO 换人；三人全倒判负
- [x] 队伍气槽规则与文档一致（共享气槽）

## 内容与表现

- [x] 4 个原创角色均可选用（名称/视觉非 SNK 资产）
- [x] Q 版剪影攻击可读；血条/气槽/队序 HUD 完整
- [x] 无商业 ROM 拆包资源

## 手感底线

- [x] 逻辑 60 FPS；本地 2P 同屏无严重掉帧（目标机）
- [x] 输入缓冲下，标注的入门连招成功率高（自动化覆盖 5A→取消必杀；本机练习验证）

## 自动化证据

```bash
godot --headless --path KofQMvp --script res://tests/run_combat_tests.gd
# ALL TESTS PASSED
python3 KofQMvp/tests/test_aabb2.py
```
