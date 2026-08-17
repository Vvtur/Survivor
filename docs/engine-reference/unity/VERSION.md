# 引擎与平台版本锁定（M0.1 — ✅ R7 已通过）

> 状态：**VERIFIED / R7 实证通过**。Unity 6 + 微信 SDK 转换 + 开发者工具预览全链路已跑通，游戏画面可渲染、帧循环正常。

## 验证通过记录
- **验证日期**：2026-08-16 04:03（GMT+8）
- **验证结果**：✅ **R7 通过** — 空场景导出 → 开发者工具导入 → Unity 帧循环正常（frame:121+），画面渲染成功
- **已知非致命警告**：URP 内部 Shader（CoreCopy/CoreBlit/HDRDebugView）在开发者工具模拟器 GPU 上编译失败（WebGL2 + 软件渲染限制），不影响 2D 游戏实际渲染，后续 M4 再处理

## 已确认（来自项目实际文件）
- **Unity 编辑器版本**：`6000.3.16f1`（Unity 6.x）
- **微信小游戏 SDK**：`com.qq.weixin.minigame` = **0.1.1**
- **URP 包版本**：`com.unity.render-pipelines.universal` = `17.3.0`
- **QFramework**：已导入，未捆绑 UniRx/R3（ADR-001 满足）
- **Color Space**：Linear
- **活动平台**：WebGL（IL2CPP / Brotli / WebGL2）

## R7 文档层核查结论（2026-08-16）
- 微信官方「推荐引擎版本」清单最高只验证到 **Unity 2022.3**，**无 Unity 6 记录**
- 但 **实证导出已通过** — Unity 6.3.16f1 + SDK 0.1.1 + 开发者工具 Stable 2.01 全链路跑通
- 结论：**超出官方验证区间但实际可用**，钉死当前组合，中途禁止升级

## 开发调试注意事项
- **本地 CDN**：开发阶段用 `python -m http.server 8080` 在 `minigame-out/webgl` 目录起本地 HTTP 服务，`game.js` 的 `DATA_CDN` 设为 `http://127.0.0.1:8080`
- **正式上线时**：需替换为真实 CDN（腾讯云 COS / 阿里云 OSS 等）
- **插件授权**：小程序后台须添加 Unity 插件（provider: `wxe5a48f1ed5f544b7`），否则 `requirePlugin` 失败导致白屏

## 已验证组合（钉死，禁升级）
| 组件 | 版本 | 备注 |
| --- | --- | --- |
| Unity | 6000.3.16f1 | Unity 6.x；超出官方清单但实证可用 |
| 微信小游戏转换插件 | 0.1.1 | com.qq.weixin.minigame |
| 微信开发者工具 | Stable 2.01.24.10520 | ⚠️ 较旧建议后续升级 |
| URP | 17.3.0 | 2D Renderer 可用 |
| AppID | wxd833d7def98f6c77 | ⚠️ 待确认权属（个人 vs 公司） |
| 验证日期 | 2026-08-16 | R7 通过 |

> 中途禁止升级任一组件；任一组件变更须重跑 M0 全链路。
