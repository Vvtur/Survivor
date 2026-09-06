# Survivor 项目长期记忆

## 项目基本面
- 2D 像素风生存小游戏（类 Vampire Survivors）。Unity **6000.3.16f1** + URP 2D（Linear），框架 QFramework 自定义版（ResKit / UIKit / AudioKit / ActionKit / FSMKit）。
- 目标平台**微信小游戏**，BuildTarget=WebGL，转换插件 `com.qq.weixin.minigame@0.1.1`，SDK 资源在 `Assets/WX-WASM-SDK-V2/`，云函数在 `WX/custom/cloudfunctions/`（save / load / invite）。
- 场景在 **`Assets/Scenes/`**：Boot（唯一进主包，buildIndex 0）+ GameStart / MainGame / SampleScene（后三个走 AB 异步加载）。注意 CODE_WIKI.md 里写的 `Assets/Res/Scenes/` 是错的。
- 分层：`Assets/Scripts/Game`（Model / System / Command / Utility）+ `Assets/Scripts/Gameplay`（表现层）+ `Assets/Res/Scripts/UI`（UIKit 面板，namespace 用 `QFramework.UI` 配 CodeGenKit）。
- 架构单向约束（QF 规范）：表现层只能 SendCommand；System 层不能 SendCommand，只能 SendEvent，由 IController 接收后转 Command。**改数据必须经过 Command。**
- WebGL 下 AB 不支持同步加载，一律用 `OpenPanelAsync` / `LoadSceneAsync` 等异步 API。

## 工具链：Unity MCP（CoplayDev/unity-mcp）
- ~~已接入~~ **2026-09-07 起改用官方 Unity CLI + Pipeline 包操控编辑器**（unityMCP 在播放模式必掉线，弃用）：
  - `unity` 命令（Unity CLI 1.0.0-beta.6）+ 项目内 Pipeline 包 0.6.0-exp.1，Survivor 编辑器服务端口 7801。
  - 核心：`unity command <cmd> --key value --project-path E:\UN\Survivor`；`editor_play/stop/focus`、`console --level error`（游标式）、`eval_file`（Roslyn 方法体）、`capture_game_view --source screen`。
  - **坑1**：编辑器失焦播放循环冻结，先 eval `Application.runInBackground=true`。
  - **坑2**：编辑器加载 WebGL AB → shader 洋红、字体方块、音频 FMOD 报错，属平台差异勿修。
  - 旧 unityMCP 备注（端口 7070、CodeDom 仅 C#6）留档：execute_code 未装 Roslyn，Unity 6 查 ScriptingDefine 要用 NamedBuildTarget。
- 已接入，包 `com.coplaydev.unity-mcp@10.1.2`。Unity 端本地服务端口 **7070**（非 README 默认的 8080）。
- 常用能力：`manage_editor`（编辑器状态 / 播放控制 / tag-layer）、`manage_scene`（层级 / 加载 / 构建设置）、`find_gameobjects`、`manage_script` / `apply_text_edits`、`read_console`（查编译错误首选）、`refresh_unity`（刷新 + 请求编译）、`execute_code`（在编辑器内跑 C# 片段）、`manage_build` / `run_tests` / `manage_camera`（截图）。
- **重要约束**：`execute_code` 未装 Roslyn（Scripting Defines 无 `USE_ROSLYN`），回退 **CodeDom = 仅支持 C# 6 语法**。需要 C# 7+ 或严格类型检查时，得先装 NuGetForUnity + Microsoft.CodeAnalysis v5.0 + SQLitePCLRaw v3.0.2 并加 `USE_ROSLYN`。
- Unity 6 API 坑：`PlayerSettings.GetScriptingDefineSymbols(BuildTargetGroup)` 重载已删除，要用 `UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(...)`。
- `manage_scene(action='get_hierarchy')` 只作用于**已加载**场景；传 `scene_name` 给未加载场景无效，会静默返回当前活动场景。
- 验证"改动是否编译通过"的可靠办法：`refresh_unity(compile='request')` → `read_console` 查 error → 或 `execute_code` 反射查类型是否存在于 Assembly-CSharp。

## 代码约定
- QF 命名规范：Command 私有字段带 `m` 前缀（如 `mCost`）。
- 玩家实际攻击力 = `GameModel.Attack`（商店升级，跨局保留）+ `GameModel.AttackDamage`（局内升级，每局重置）。
- 升级经验公式唯一出处：**`GameModel.ExpToNextLevel`（= Level * 2 + 1）**，LevelUpSystem 与 PlayerInfoPanel 共用，勿在别处重写公式。
- 常驻面板 PlayerInfoPanel 直接挂在 MainGame 场景预制体上，**不走 UIKit 生命周期**（OnInit/OnOpen/OnDestroy 不会被 QF 调用），订阅必须放 Unity 的 Awake，注销放 Unity 的 OnDestroy。GameStartPanel 同为场景直挂。
- 扣血走 `DamagePlayerCommand`；击杀数走 `KillEnemyCommand` 内 `KillCount++`。表现层禁止直写 Model。

## UI 动效（2026-08-28 落地）
- **DOTween 已在项目里**：`Assets/Plugins/Demigiant/DOTween/DOTween.dll`，UI 模块启用（无人定义 `DOTWEEN_NOUI`），`CanvasGroup.DOFade` / `Image.DOFillAmount` / `DOScale/DOPunchScale` 可直接用。动画统一放 `Assets/Res/Scripts/UI/UIPanelAnim.cs`（PlayOpen / PlayClose / PlayIntro 扩展，全部 `SetUpdate(true)`，CanvasGroup 运行时自动补挂）。
- **UIKit 生命周期关键时序**：`UIPanel.Show()` 先 `SetActive(true)` 再调 `OnShow()` → 打开动画只能挂 `OnShow()`（OnOpen 在 SetActive 之前，禁启 tween）。UIKit 关闭是同步 Destroy → 关闭动画必须"先播 → onComplete 里再 CloseSelf/Hide/ClosePanel"。ShopPanel 的关闭语义是 `Hide()`（不是 CloseSelf，保持 Single 复用）。
- 需要跨面板复用知道的事实：升级面板在 timeScale=0 时打开，恢复 timeScale=1 要放在关闭动画 onComplete 里；GameOver 面板打开协程需要常驻宿主（PlayerController.GameOver 里本对象马上 SetActive(false)）。**注意别挂 GameRoot——它没有 `DontDestroyOnLoad`，会随 Boot 场景销毁**，应挂 `UIKit.Root`（见下方过场章节）。
- 预制体批量改版工作流（已验证）：先改 Designer.cs（手加 `[SerializeField] public` 字段 + ClearUIComponents 置空）→ refresh_unity 编译 → `execute_code` 里 `PrefabUtility.LoadPrefabContents` 建节点/摆 RectTransform（FZSTK SDF 字体在 `Assets/Res/Art/Font/FZSTK SDF.asset`）→ `SerializedObject.FindProperty("字段名").objectReferenceValue` 接线 → `SaveAsPrefabAsset` → 场景实例根 override 用 OpenScene(Additive)+改根 RectTransform+SaveScene+CloseScene 清理（注意 CloseScene 会销毁实例，先取 name）。新增贴图（如 `Assets/Res/Art/Graph/UI/ExpGradient.png` 渐变条）后**必须重打 AB**。

## UI 字号与手机适配（2026-08-29 落地）
- **参考分辨率**：1280×720（横屏 16:9）；CanvasScaler 默认 `match=0`（匹配高度），在更窄的屏幕（16:10、4:3）会横向裁切。
- **横屏锁定**：`ProjectSettings.defaultInterfaceOrientation = LandscapeLeft`（值 3），关闭 portrait autorotate；微信侧 `game.json: deviceOrientation = landscape` 已正确。
- **字号规范（5 档，最小 24）**：L1 标题 96、L2 按钮主文字 40、L3 HUD 焦点（时间）36、L4 HUD 关键（等级/生命）30、L5 HUD 次要（击杀/金币/攻击/经验数字）24。TMP 的 `m_fontSizeMin/Max` 统一改为 `8 ~ 字号×1.5`。
- **适配脚本**：`Assets/Res/Scripts/UI/UICanvasAdapter.cs` 提供两个组件：
  - `UICanvasAdapter`：挂在场景 Canvas 上，屏幕比 16:9 更窄时把 `matchWidthOrHeight` 从 0 切到 1，防横向裁切。
  - `UISafeAreaFitter`：挂在面板根节点上，按 `Screen.safeArea` 缩进内容，避让刘海/圆角/Home 指示条。PlayerInfoPanel 预制体已挂。
- **踩坑备忘**：给预制体 AddComponent 时若目标脚本尚未编译完成，会生成 `m_Script: {fileID: 0}` 的 missing script，且 `SaveAsPrefabAsset` 会报错。必须等脚本编译通过后再加；修复时可用 `GameObjectUtility.RemoveMonoBehavioursWithMissingScript` 清理，必要时直接改 YAML 补 `m_Script` 的 guid。

## 微信小游戏出包与 CDN 缓存（2026-09-07 定位）
- 出包链路：Build WebGL → `Assets/WX-WASM-SDK-V2` 转换 → 输出到 `E:/UN/Survivor/WX/webgl`（含 StreamingAssets 拷贝）+ `WX/minigame`（devtools 打开的就是它）。`minigame-out/` 是更早期的旧产物，与 devtools 无关。
- CDN 地址写在 `Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset` 的 `CDN` 字段，转换后写死进 `WX/minigame/game.js` 的 `DATA_CDN`。运行时 AB 走 CDN，不走包内文件。
- **最大坑：devtools/真机有文件缓存** `.../微信开发者工具/User Data/<profile>/WeappSimulator/WeappFileSystem/<openid>/<appid>/usr/__GAME_FILE_CACHE/StreamingAssets/AssetBundles/WebGL/`，`asset_bundle_config.bin` 命中缓存就不会重新下载 → 表现为"UI 是旧的 + 新资源 Failed to Create Res"。重打 AB / 传 CDN 后必须清缓存（devtools：清缓存→清除数据缓存与文件系统缓存；真机：删小程序或清缓存），否则改多少都不生效。
- 排查手法：`md5sum` 对比本地 AB、CDN `curl` 下来的文件、缓存目录里的文件；再用 `grep -a "资源名" asset_bundle_config.bin` 判断某资源是否在配置里。

## 过场加载动画（2026-08-31 简化重构后）
- **全部逻辑在单文件** `Assets/Res/Scripts/UI/LoadingPanel.cs`。对外只有两个符号：`LoadingPanel.SwitchScene(sceneName, loaderFactory, onComplete)` 和 `LoadingPanel.IsTransitioning`（按钮防重入）。调用点：GameStartPanel / GameOverPanel / GameRoot。
- **跨场景协程一律挂 `UIKit.Root`**：`UIRoot.Instance` 在 `UIRoot.cs:43` 有 `DontDestroyOnLoad`，且 Boot/GameStart/MainGame 三个场景**都没有预置 UIRoot 实例**（必然走 Instantiate 分支）→ 是可靠的常驻宿主。原 `SceneTransition.cs` 已删除（双层协程 + 宿主中途切换的写法废止）。
- **拿 UIKit 面板实例用 `UIManager.Instance.OpenUIAsync(keys, callback)`**（public，回调直接送 IPanel），**不要** `yield UIKit.OpenPanelAsync`（内部 `while(!loaded) yield WaitForEndOfFrame`，编辑器 Game View 停止渲染时永久挂死），也不要每帧 `FindFirstObjectByType` 扫描。资源加载失败时回调**不会**触发（`UIKitWithResKitInit.cs:53` 的 `if (success)`），所以必须配超时兜底。
- **圆形遮罩是零 shader 方案**：运行时生成 1024² 抗锯齿圆 Sprite 挂到 `Img_Mask` 的 Image 上，靠 UGUI `Mask` 组件（stencil）裁剪子物体 `Img_Wipe`；动效只驱动 `Img_Mask.sizeDelta`（0 → 屏幕对角线+2，保证盖住四角）。圆 Sprite 用 static 缓存，只生成一次。
- **进度必须分两段**：AB 模式下 `AsyncOperation` 要等场景 AB 加载完才在回调里创建（`IResLoaderExtensions.cs:296-305`），op 到手前只能假爬升（封顶 85%），到手后跟 `op.progress` 映射到 20%~100%。
- **⚠️ LoadingPanel 未打进任何 AB**（所有 manifest 都搜不到）→ 真机/小游戏上每次过场先白等 10s 超时再无动画裸切，只有编辑器 SimulationMode 能正常看到动画。目录 meta `Assets/Res/Art/UIPrefab.meta` 已标记 `uiprefab`，**重打 AB 即可修复**（用户暂缓处理）。
