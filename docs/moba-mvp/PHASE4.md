# Phase 4 — 3v3 与补位 Bot

## 目标

3v3 人数、每队 3 出生点、缺人自动补 Bot（跟线 / 进攻 / 残血回撤 / 放技能）。

## 用法

1. Lobby 选 **3v3**，勾选 **补位 Bot**
2. Host → Start Match
3. Host 一人开局会补 5 个 Bot；两人则补 4 个，以此类推
4. 1v1 + Fill Bots：补 1 个对面 Bot，可单机练手

关闭 Fill Bots 则只进真人。

## Bot 行为

- 无敌人：沿中路向对方水晶推进
- 进仇恨：靠近并普攻 / 放技能
- 血量 ≤ 28%：撤回泉水方向

## 本机

**MobaMVP → Setup Phase 0–4 Scenes And Prefabs** 后重跑。

## 单测

`BotBrain` 决策与补位人数计算见 `Phase4BotTests`。
