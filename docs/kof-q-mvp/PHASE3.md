# Phase 3 — 小队 3v3 + 角色数据化

## 状态：完成

## 目标

选人界面为双方各选 **3 人不重复顺序**；击倒后换下一人跳入；**4 个 CharacterDef** 均可玩。

## 任务清单

- [x] `TeamOrder`：3 槽位、当前 index、剩余人数（`FightSimulator`）
- [x] 选人 UI：点选顺序、确认
- [x] KO → 退场 → 下一人入场（入场无敌）
- [x] 三人全倒 → 胜负
- [x] C01～C04 JSON 数据驱动
- [x] HUD 显示队序与剩余

## 气槽继承

**整队共享一条气槽**，换人不清零。

## 下一阶段

Phase 4：Training / Arcade / CPU。
