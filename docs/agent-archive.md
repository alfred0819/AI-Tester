# Agent 对话归档索引

合并进 `main` 并删除功能分支后的对应 Cloud Agent 对话状态（2026-10-19）。

| 对话 | bcId | 原分支 | 目录 | 归档状态 |
|------|------|--------|------|----------|
| [Q版拳皇2002游戏](https://cursor.com/agents/bc-01a14520-3896-7d79-b26a-28eb5f5b8211) | `bc-01a14520-3896-7d79-b26a-28eb5f5b8211` | `cursor/kof-q-mvp-plan-8211` | `KofQMvp/` | 本对话已收尾；请在 Cursor UI 点 Archive |
| [大模型创造过程](https://cursor.com/agents/bc-01a02f68-ddc8-7d82-8082-a5388a163185) | `bc-01a02f68-ddc8-7d82-8082-a5388a163185` | `cursor/bigmodel-learn-doc-3185` | `bigmodel_learn/` | 待归档 |
| [Wangzhe rongyao replication](https://cursor.com/agents/bc-01a0b5a3-261d-7e6f-b7f4-0e17e159bedd) | `bc-01a0b5a3-261d-7e6f-b7f4-0e17e159bedd` | `cursor/moba-mvp-unity-plan-bedd` | `MobaMVP/` | 已归档 |
| [热点新闻技能](https://cursor.com/agents/bc-01a04c0f-ec87-7cd7-bbb2-c6af6a00c49e) | `bc-01a04c0f-ec87-7cd7-bbb2-c6af6a00c49e` | `cursor/today-hot-news-skill-c49e` | `hot-news/` | 已归档 |
| [(setup env)](https://cursor.com/agents/bc-01a04c0f-36bf-7339-8243-6a2152e56bb7) | `bc-01a04c0f-36bf-7339-8243-6a2152e56bb7` | `cursor/setup-dev-environment-9ecf` | `.cursor/environment.json` | 已归档 |
| [Explore MOBA MVP gaps](https://cursor.com/agents/bc-18852481-8878-57c9-b619-369938a75200) | `bc-18852481-8878-57c9-b619-369938a75200` | （无） | `MobaMVP/` | 待归档 |

## 已删除的远程分支

- `cursor/bigmodel-learn-doc-3185`
- `cursor/intl-news-translation-6bb7`
- `cursor/kof-q-mvp-plan-8211`
- `cursor/moba-mvp-unity-plan-bedd`
- `cursor/setup-dev-environment-9ecf`
- `cursor/short-drama-skill-c49e`
- `cursor/today-hot-news-skill-c49e`
- `paseo/2dCameFGT`

远端现仅保留 `main`。

## 用 API 批量归档（可选）

在 [Cursor Dashboard → API Keys](https://cursor.com/dashboard?tab=integrations) 创建密钥后：

```bash
export CURSOR_API_KEY=...
for id in \
  bc-01a02f68-ddc8-7d82-8082-a5388a163185 \
  bc-18852481-8878-57c9-b619-369938a75200 \
  bc-01a14520-3896-7d79-b26a-28eb5f5b8211
do
  curl -sS -X POST "https://api.cursor.com/v1/agents/${id}/archive" -u "$CURSOR_API_KEY:" -d '{}'
  echo " archived $id"
done
```
