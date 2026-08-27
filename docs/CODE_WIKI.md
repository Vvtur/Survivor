# Survivor 项目 Code Wiki

> 本文档是针对 **E:\UN\Survivor** 仓库的结构化代码手册，覆盖：整体架构、模块职责、关键类与函数、依赖关系、运行方式。
> 若仓库发生结构性变更，请同步更新本文档。

---

## 目录

1. [项目概览](#1-项目概览)
2. [整体架构](#2-整体架构)
3. [目录结构总览](#3-目录结构总览)
4. [模块职责](#4-模块职责)
5. [关键类与函数说明](#5-关键类与函数说明)
6. [依赖关系](#6-依赖关系)
7. [运行方式](#7-运行方式)
8. [全局约定与注意事项](#8-全局约定与注意事项)

---

## 1. 项目概览

| 项 | 内容 |
| --- | --- |
| 项目类型 | 2D 像素风**生存类小游戏**（类 Vampire Survivors） |
| 引擎 | Unity **6000.3.16f1**（Unity 6.x，URP 17.3.0 2D Renderer，Linear 色彩空间） |
| 核心框架 | **QFramework**（自带 Custom 版本，含 ResKit / UIKit / AudioKit / ActionKit / FSMKit / IOC / Event 等子模块） |
| 目标平台 | **微信小游戏**（WebGL / IL2CPP / Brotli / WebGL2），可同时在编辑器中直接运行 |
| 玩法循环 | 移动摇杆操控角色 → 自动攻击范围内的敌人 → 击杀掉落金币/经验宝石 → 拾取经验升级 → 三选一能力 → 无限刷怪、怪物随时间变强 → 存活 120 秒胜利 / 血量归零失败 |
| 微信能力 | 云存档（Last-Write-Win）、好友排行榜（开放数据域）、分享、头像昵称授权、邀请有礼（云函数） |
| 版本锁定 | 引擎 + 微信 SDK（com.qq.weixin.minigame **0.1.1**）+ 微信开发者工具 Stable **2.01**（R7 实证通过，禁止中途升级） |

---

## 2. 整体架构

### 2.1 分层模型（QFramework 标准三层）

项目遵循 QFramework 的 **Command / Query / Event / Architecture** 依赖倒置架构，所有业务代码收敛在 `Assets/Scripts` 与 `Assets/Res/Scripts`：

```
┌────────────────────────────────────────────────────────────┐
│ 表现层（MonoBehaviour 场景脚本）                               │
│   Gameplay/: PlayerController Enemy Weapon Gem Gold           │
│   UI/: XxxPanel + Designer（UIKit 面板）                       │
└───────────────┬──────────────────────────────────────────────┘
                │ 发 Command / 订阅 Event / 读 Model（依赖倒置：面向接口）
┌───────────────▼──────────────────────────────────────────────┐
│ 架构层 GameArchitecture（唯一总入口，Interface 静态访问）        │
│                                                              │
│  System（规则逻辑，无数据）→ Model（数据，BindableProperty）     │
│    GameManagerSystem        GameModel                         │
│    LevelUpSystem                                              │
│    WaveSystem                                                 │
│    AbilityPoolSystem                                          │
│    GameAssetsSystem                                           │
│    CloudSaveSystem                                            │
│    WXPlatformSystem                                           │
│    InviteSystem                                               │
│                                                              │
│  Utility（工具）: Storage / DamageTextPool / AudioNames        │
└───────────────┬──────────────────────────────────────────────┘
                │ 加载
┌───────────────▼──────────────────────────────────────────────┐
│ 资源层 ResKit（AssetBundle）                                   │
│   场景(GameStart/MainGame) / 预制体 / SO配置 / 音频 / UI面板    │
│   微信平台层：WX-WASM-SDK + WX/custom 云函数 + open-data        │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 架构初始化流程（启动时序）

```
Boot 场景（唯一打进主包的场景，见 EditorBuildSettings）
    └─ GameRoot（常驻不销毁）
         └─ Start → 协程 Boot()
              ├─ ResKit.InitAsync()          // WebGL 必须异步初始化
              └─ GameArchitecture.Interface  // 首次访问触发架构 Init：
                    ├─ RegisterSystem<IGameManagerSystem>(GameManagerSystem)
                    ├─ RegisterSystem<ILevelUpSystem>(LevelUpSystem)
                    ├─ RegisterSystem<IAbilityPoolSystem>(AbilityPoolSystem)
                    ├─ RegisterSystem<IGameAssetsSystem>(GameAssetsSystem)
                    ├─ RegisterSystem<IWaveSystem>(WaveSystem)
                    ├─ RegisterModel(GameModel)
                    ├─ RegisterUtility<Storage>(Storage)
                    ├─ RegisterSystem<ICloudSaveSystem>(CloudSaveSystem)   // 依赖 Model+Storage 已注册
                    ├─ RegisterSystem<IWXPlatformSystem>(WXPlatformSystem)
                    └─ RegisterSystem<IInviteSystem>(InviteSystem)         // 依赖 CloudSave 先注册
              └─ mSceneLoader.LoadSceneAsync("GameStart")   // AB 场景加载
```

**关键设计点：**
- 只有 **Boot** 场景在主包，`GameStart` / `MainGame` 场景全部在 AB 中，由 GameRoot 持有的 `ResLoader` 加载。
- **注册顺序即依赖顺序**：`GameModel`/`Storage` 必须先于 `CloudSaveSystem`（其 `OnInit` 同步 `GetModel/GetUtility`）；`InviteSystem` 必须晚于 `CloudSaveSystem`（复用其 `WX.cloud.Init`）。
- 场景切换使用 **OpenPanelAsync / LoadSceneAsync 等异步 API**——WebGL 下 AB 不支持同步加载，同步 API 首次加载必失败（代码注释中多次强调）。

### 2.3 事件系统（EventBus 解耦）

`Assets/Scripts/Game/GameEvent.cs` 定义了 3 个全局结构体事件：

| 事件 | 发出方 | 订阅方 | 语义 |
| --- | --- | --- | --- |
| `EnemyKilledEvent` | `KillEnemyCommand` | （目前无订阅者，预留） | 敌人被击杀 |
| `GameWinEvent` | `WaveSystem`（存活时间到） | `PlayerController.OnGameWin` | 游戏胜利 |
| `LevelUpEvent` | `LevelUpSystem`（经验达标） | `PlayerController.OnLevelUp` | 角色升级，打开三选一能力面板 |

数据流范式：**玩法脚本 → Command 改 Model → Register 回调落盘/刷新 UI → System 监听 Model 变化发 Event → 表现层处理**。

---

## 3. 目录结构总览

```
E:\UN\Survivor/
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Game/                     # 架构核心（QFramework 分层）
│  │  │  ├─ GameArchitecture.cs    # 架构定义 + 注册中心
│  │  │  ├─ GameRoot.cs            # 启动入口（Boot 场景常驻）
│  │  │  ├─ GameEvent.cs           # 全局事件定义
│  │  │  ├─ AudioNames.cs          # 音频资源名常量
│  │  │  ├─ Models/GameModel.cs    # 全局游戏数据模型
│  │  │  ├─ Systems/               # 7+1 个系统（规则层）
│  │  │  ├─ Commands/              # 7 个命令（写数据入口）
│  │  │  └─ Utility/               # Storage 本地存储 / DamageTextPool
│  │  ├─ Gameplay/                 # 玩法 MonoBehaviour（表现层）
│  │  │  ├─ PlayerController / Enemy / Weapon / Gem / Gold
│  │  │  ├─ WaveDriver / BGController（无限地图）/ Shop
│  │  │  └─ DynamicJoystick / FloatingJoystick（虚拟摇杆）
│  │  ├─ Editor/FixBuiltinSprites.cs
│  │  └─ InputSystem/InputSystem_Actions.cs   # InputSystem 生成代码
│  ├─ Res/
│  │  ├─ Scripts/UI/               # 全部 UI 面板（XxxPanel.cs + Designer.cs）
│  │  ├─ GameRes/
│  │  │  ├─ Ability/               # AbilityConfig / AbilityDatabase（脚本）
│  │  │  ├─ Data/                  # SO 资产：GameConfigDate、AbilityDatabase、6 个能力资产
│  │  │  └─ GameConfig/GameConfig.cs
│  │  ├─ Art/                      # 音频 / 角色 / 字体 / 贴图 / 预制体 / UI 预制体
│  │  └─ Scenes/                   # Boot / GameStart / MainGame / SampleScene
│  ├─ QFramework/                  # QFramework 框架源码（自定义版，含 ResKit/UIKit/AudioKit 等）
│  ├─ WX-WASM-SDK-V2/              # 微信小游戏 SDK 插件资源
│  ├─ WebGLTemplates/              # 微信小游戏 WebGL 模板（WXTemplate 系列）
│  └─ StreamingAssets/AssetBundles/WebGL/  # AB 构建产物（构建后生成）
├─ Packages/manifest.json          # Unity 包依赖
├─ ProjectSettings/                # 工程设置（ProjectVersion.txt = 6000.3.16f1）
├─ WX/                             # 微信小游戏侧
│  ├─ custom/cloudfunctions/       # 云函数（invite / load / save）★可部署
│  ├─ minigame/                    # 微信开发者工具导入目录（转换产出）
│  ├─ webgl/                       # WebGL 构建产物原始目录
│  └─ restore-custom.bat/.ps1      # 恢复 custom 目录脚本
├─ minigame-out/minigame/          # 打包输出目录（本地调试用）
├─ docs/                           # 各类设计文档（architecture / engine-reference 等）
└─ AssetBundles/WebGL/             # AB 构建缓存
```

---

## 4. 模块职责

| 模块 | 目录 | 职责 |
| --- | --- | --- |
| **架构核心** | `Assets/Scripts/Game` | 定义架构、注册 System/Model/Utility、启动引导、全局数据、事件、命令 |
| **玩法层** | `Assets/Scripts/Gameplay` | 玩家移动/攻击、敌人 AI 与掉落、武器 AOE、拾取物、无限地图、刷怪驱动、摇杆输入 |
| **UI 层** | `Assets/Res/Scripts/UI` | 开始/升级/结算/商店/排行榜/邀请/玩家信息面板，全部基于 UIKit + ResKit AB 加载 |
| **配置资产** | `Assets/Res/GameRes` | `GameConfig`（平衡数值）、`AbilityConfig` + `AbilityDatabase`（能力池） |
| **资源层** | `Assets/Res/Art` | 预制体（Player/Enemy/Weapon/Gem/Gold + UI 面板）、音频、字体、贴图 |
| **音频管理** | `AudioNames.cs` + `AudioKit` | 统一音频资源名常量，AudioKit 经 ResKit 异步加载播放 |
| **本地存储** | `Storage.cs` | 微信小游戏 `wx.setStorageSync` 封装，编辑器/其他平台回退 `PlayerPrefs` |
| **微信平台** | `WXPlatformSystem` + `WX/` | 分享、头像昵称授权、排行榜上报（托管数据）、云开发 |
| **云后端** | `WX/custom/cloudfunctions` | `save`/`load`（云存档）、`invite`（邀请校验/领奖），微信云开发 Node 函数 |
| **开放数据域** | `WX/minigame/open-data/index.js` | 好友排行榜渲染（主域只显示 sharedCanvas 贴图） |
| **框架本体** | `Assets/QFramework` | QFramework 自定义发行版：ResKit（AB 资源）、UIKit（面板）、AudioKit、ActionKit、FSMKit、IOCKit 等 |

---

## 5. 关键类与函数说明

### 5.1 架构层

#### `GameArchitecture`（`Assets/Scripts/Game/GameArchitecture.cs`）
`Architecture<GameArchitecture>` 的实例，全项目唯一架构。`Init()` 中按依赖顺序注册全部 System/Model/Utility（见 2.2）。外部统一通过 `GameArchitecture.Interface` 访问。

#### `GameRoot`（`Assets/Scripts/Game/GameRoot.cs`）
挂在 **Boot 场景** 的常驻启动组件。
- `Boot()`：`ResKit.InitAsync()` → 触碰 `GameArchitecture.Interface` 完成初始化 → `mSceneLoader.LoadSceneAsync("GameStart")`。
- 注释中保留了"场景切换统一入口 + 面板统一清理"的设计（`OnSceneLoaded` 已注释停用，目前场景加载改由各面板各自持有 ResLoader 完成）。

#### `GameModel`（`Assets/Scripts/Game/Models/GameModel.cs`）
数据层，全部字段为 `BindableProperty<T>`。

| 属性 | 类型 | 说明 |
| --- | --- | --- |
| `AliveEnemies` / `PlayerGem` | int | 场上敌人数 / 本局拾取宝石数 |
| `Level` / `Exp` | int / int | 等级（升 1 级需 `Level*2+1` 经验） |
| `AttackDamage` / `Attack` | float | 局内攻击加成 / **商店永久攻击**（跨局保留，实际攻击 = 二者之和） |
| `MoveSpeed` / `MaxHp` / `HP` | float/int/int | 移速 / 最大血量 / 当前血量 |
| `AttackInterval` / `AttackRadius` | float | 攻击间隔（能力缩短，下限 0.05s）/ 攻击范围（能力扩大） |
| `Money` / `WeaponCount` | int / int | 金币（**跨局保留**，自动存档）/ 同时生成的剑数 |
| `SpawnInterval` / `MaxAliveEnemies` / `EnemyPowerPerSecond` / `SurviveTimeToWin` | float/int/float/float | 无限刷怪参数（从配置复制，运行中固定；胜利时间 0=不设胜利） |

关键函数：
- `void ResetRunData()`：每局开始重置局内成长属性回配置初始值（金币/商店攻击保留）。
- `void ApplyCloudSave(SaveData cloud, long cloudTsSec)`：Last-Write-Win 云档合并，云端时间戳较新才覆盖 `Money`/`Attack` 并同步 `SaveTs`。
- `OnInit()`：异步加载 `GameConfigDate.asset`（AB 名 `data`）缓存初始值；挂 `Money`/`Attack` 的本地持久化 Register；调一次 `ResetRunData`。

### 5.2 System 层（规则逻辑，无数据）

| System | 文件 | 职责与关键成员 |
| --- | --- | --- |
| `GameManagerSystem` | `Systems/GameManagerSystem.cs` | 游戏管理占位（胜利判定已迁移至 WaveSystem，OnInit 为空） |
| `LevelUpSystem` | 同上文件 | 监听 `model.Exp`：`newExp >= Level*2+1` 时扣经验、升级、发 `LevelUpEvent` |
| `WaveSystem` | `Systems/WaveSystem.cs` | 无限刷怪引擎。`OnUpdate()`（WaveDriver 每帧驱动）：累计 `ElapsedTime`、到 `SurviveTimeToWin` 发 `GameWinEvent`、按 `SpawnInterval`（每 8 次生成有几率 ×0.95 加速）生成敌人；`SpawnEnemy()` 按 `1 + ElapsedTime * EnemyPowerPerSecond` 计算强度系数；`GetSpawnPosition()` 屏幕四边外随机出生；`ResetRun()` 每局归零计时与间隔（防跨局残留） |
| `AbilityPoolSystem` | `Systems/AbilityPoolSystem.cs` | 异步加载 `AbilityDatabase`（AB 名 `data`）；`RollOptions(int count)` 洗牌抽 N 个不重复能力 |
| `GameAssetsSystem` | `Systems/GameAssetsSystem.cs` | 异步预加载 Gem/Gold/Enemy/Weapon 预制体并长期持有；`SpawnGem/SpawnGold/SpawnEnemy` 直接实例化；`SpawnWeapon(targetEnemyPos, damage, target)` 从 `SafeObjectPool<Weapon>` 取出并 `Init`（池工厂方法在 Weapon 加载完成后注册） |
| `CloudSaveSystem` | `Systems/CloudSaveSystem.cs` | 云存档。启动 `load` 拉云档 → `ApplyCloudSave` 合并；监听 Money/Attack 变化 → `MarkDirty` → 静默 2s 防抖 → `save` 推送。含 `SaveData/CloudSaveResponse/SaveCallData` 三个序列化结构。`EnvId = cloud1-d8gevn4facad5c770`。**仅在微信真机（UNITY_WEBGL 且非编辑器）生效** |
| `WXPlatformSystem` | `Systems/WXPlatformSystem.cs` | `ReportSurviveTime(seconds)`（破纪录才托管，key=`bestTime`）、`ShareToFriend()`（标题带最佳存活时间）、`PromptAuthIfNeeded(x,y,w,h,onTap)`（以微信 GetSetting 核实授权态，未授权弹透明授权按钮） |
| `InviteSystem` | `Systems/InviteSystem.cs` | 邀请有礼。`RewardMoney=100`。`ShareWithInvite`（分享带 `inviter=自身openid`）、`CheckStatus`（云端查 invited/claimed）、`ClaimReward`（云端校验通过才加钱，code: 0成功/1已领/2无资格/-1网络）、`TrackInviteFromLaunch`（冷启动查 `GetLaunchOptionsSync` + JS 快照兜底上报 inviter）。数据全部以云函数 `invite` 记录为准 |

### 5.3 Command 层（写数据唯一入口）

| Command | 文件 | 作用 |
| --- | --- | --- |
| `BuyCommand(int cost)` | `Commands/BuyCommand.cs` | 商店购买：金币够则扣钱、`Attack += 1`（永久攻击） |
| `ChooseAbilityCommand(AbilityConfig)` | `Commands/ChooseAbilityCommand.cs` | 按 `AbilityEffect` 修改 Model：AttackUp→攻击、SpeedUp→移速、MaxHpUp→血量+回血、AttackSpeedUp→缩短间隔(下限0.05)、AttackRangeUp→扩大范围、MoreWeapon→剑数+1 |
| `SpawnEnemyCommand(pos, power)` | `Commands/SpawnEnemyCommand.cs` | 用 GameAssetsSystem 实例化敌人并按强度系数放大 `HP` |
| `EnemySpawnCommand` | `Commands/EnemySpawnCommand.cs` | 敌人出生登记：`AliveEnemies++`（由 `Enemy.Start` 发出） |
| `KillEnemyCommand` | `Commands/KillEnemyCommand.cs` | `AliveEnemies--` + 发 `EnemyKilledEvent`（由 `Enemy.Die` 发出） |
| `PickupGoldCommand` | `Commands/PickupGoldCommand.cs` | `Money++`（Register 自动落盘跨局保留） |
| `PlayerGetGemCommand` | `Commands/PlayerGetGemCommand.cs` | `PlayerGem++` 且 `Exp++`（触发升级判断） |

### 5.4 Utility 层

| 类 | 文件 | 说明 |
| --- | --- | --- |
| `Storage : IUtility` | `Utility/Storage.cs` | `SaveInt/GetInt/SaveFloat/GetFloat/SaveString/GetString`；微信真机走 `WeChatWASM.WX.Storage*Sync`，其余走 `PlayerPrefs` |
| `DamageTextPool : MonoBehaviour` | `Utility/DamageTextPool.cs` | 伤害飘字对象池（挂在 MainGame 场景，随场景销毁）。`Show(worldPos, damage, color?)`；`Update` 统一驱动，`unscaledDeltaTime` 保证暂停时飘完，零 GC |
| `AudioNames` | `Game/AudioNames.cs` | 全部音频资源名常量（BGM：`bgm_battle`；音效 15 个：attack/hit/enemy_die/pickup/level_up/game_over/game_start/victory/button_click/panel_open/panel_close/pause/skill/player_hurt/option_switch） |

### 5.5 玩法层（表现层 MonoBehaviour）

#### `PlayerController`（`Assets/Scripts/Gameplay/PlayerController.cs`）
`ViewController` + `IController`，玩家主体。
- 属性统一读缓存 `GameModel`：`Speed`/`AttackDamage`(=商店攻击+局内攻击)/`AttackInterval`/`AttackRadius`。
- `InitAnimFSM()`：QF FSM 驱动 Idle/Walk 动画（`Animator.Play(hash)`）。
- `Update()`：读 InputSystem 摇杆输入 → `rigidbody.linearVelocity = move * Speed` → x 方向翻转 sprite → 切动画 → `TryAttackAllInRange()`。
- `TryAttackAllInRange()`：攻击 CD 判断 → `Physics2D.OverlapCircle` 无 GC 命中 → 范围内敌人按距离排序 → 取前 `WeaponCount` 个各 `SpawnWeapon`。
- `OnTriggerStay2D`：身体碰撞敌人扣血（1s 受击间隔），HP≤0 → `GameOver()`。
- `OnLevelUp(LevelUpEvent)`：播升级音效、`timeScale=0` 暂停、抽 3 个能力、`OpenPanelAsync<GameLevelUpPanel>`。
- `OnGameWin(GameWinEvent)`：播胜利音效 → `GameOver()`。
- `GameOver()`：上报存活时间到排行榜（`IWXPlatformSystem.ReportSurviveTime`）、停 BGM、`OpenPanelAsync<GameOverPanel>`、主角失活、`timeScale=0`。
- `LateUpdate`：`SmoothDamp` 摄像机跟随（只跟随 XY）。

#### `Enemy`（`Assets/Scripts/Gameplay/Enemy.cs`）
- `Speed=2f`、`HP=5`（SpawnEnemyCommand 可按强度放大）。
- `Start()`：按 Tag 找玩家、发 `EnemySpawnCommand` 登记。
- `Update()`：朝玩家方向移动。
- `TakeDamage(float)`：受击闪红 + 伤害飘字 + 扣血，HP≤0 → `Die()`。
- `Die()`：`KillEnemyCommand` 登记击杀 → ActionKit 1s 渐隐 → `DropLoot()`（50% 掉金币 / 50% 掉宝石）→ 销毁。

#### `Weapon`（`Assets/Scripts/Gameplay/Weapon.cs`）
`IPoolable` + `IPoolType` 的近战武器（SafeObjectPool 复用）。
- `Init(targetEnemyPos, damage, target)`：定位目标左侧（带随机抖动）、注入伤害与目标、播 Attack 动画。
- `Update()`：跟随目标移动；动画播放完毕自动 `Recycle2Cache()` 回池。
- `Attack()`：**动画事件（Attack 帧）调用**。以武器 BoxCollider bounds 做 `OverlapBox` AOE，去重收集敌人，目标敌人必命中，超 `MaxHitCount=5` 按距离取最近，逐个 `TakeDamage`，有命中才播音效。

#### `Gem` / `Gold`（拾取物）
- `Gem.OnTriggerEnter2D(Player)`：`PlayerGetGemCommand`（经验+1）+ 拾取音效 + 销毁。
- `Gold.OnTriggerEnter2D(Player)`：`PickupGoldCommand`（金币+1）+ 拾取音效 + 销毁。

#### `WaveDriver`（`Assets/Scripts/Gameplay/WaveDriver.cs`）
挂在 MainGame 场景空物体，`Update()` 每帧调 `mWaveSystem.OnUpdate()`，场景卸载自动停止驱动。

#### `BGController`（`Assets/Scripts/Gameplay/BGController.cs`）
无限滚动背景：3×3 共 9 块，玩家越出中心块半块宽/高即触发行列换位（数组下标随物理位置整体轮转，防残留引用），背景视觉无限延伸。

#### `Shop`（`Assets/Scripts/Gameplay/Shop.cs`）
挂商店按钮，点击 `OpenPanelAsync<ShopPanel>`。

#### 虚拟摇杆（`DynamicJoystick.cs` / `FloatingJoystick.cs`）
均继承 `OnScreenControl`，输出到 `<Gamepad>/leftStick`，`PlayerController` 的 InputSystem `Player.Move` 直接读取，无需改动。`DynamicJoystick` 是"点哪 BG 去哪"；`FloatingJoystick` 是"左半屏长按浮出"（`activeArea=0.5`）。

### 5.6 UI 面板层（`Assets/Res/Scripts/UI/`，QFramework UIKit）

| 面板 | 入口 | 职责 |
| --- | --- | --- |
| `GameStartPanel` | Boot 场景加载后由 GameRoot 打开 | 开始游戏（`LoadSceneAsync("MainGame")`）、排行榜（`LeaderboardPanel`）、邀请（`InvitePanel`）；`OnOpen` 处理冷启动邀请上报 + 头像昵称授权前置弹窗；0.5s 防误触 |
| `PlayerInfoPanel` | MainGame（PlayerController.Start 注释提及，当前由场景挂载） | 实时显示 HP/MaxHp、Exp/需求经验（公式 `Level*2+1`）、等级、攻击；订阅 5 个 Model 属性，`UnRegisterAll` 防复用泄漏 |
| `GameLevelUpPanel` | `PlayerController.OnLevelUp` | `GameLevelUpPanelData.Options` 传入 3 个能力；异步加载 `Btn_Option` 预制体生成选项项；点击 → `ChooseAbilityCommand` → 恢复 `timeScale=1` → 关面板 |
| `AbilityOptionItem` | 升级面板子项 | `SetData(AbilityConfig)` 填充图标+名称+描述 |
| `GameOverPanel` | `PlayerController.GameOver` | 重新开始（`LoadSceneAsync("GameStart")`，先恢复 timeScale） |
| `ShopPanel` | `Shop` 按钮 | `BuyCommand(1)` 花 1 金币加 1 永久攻击；`Money` 变化实时刷新 |
| `LeaderboardPanel` | GameStart/Btn_Board | 微信开放数据域：`WX.ShowOpenData(tex, x,y,w,h)` 把沙盒画布贴到 RawImage（uvRect 垂直镜像 `(0,1,1,-1)` 修正倒置），关闭时 `WX.HideOpenData`；分享按钮走邀请链路 |
| `InvitePanel` | GameStart/Btn_Invite | 分享（`ShareWithInvite`）+ 领取（`ClaimReward`）；打开即 `CheckStatus` 刷新按钮态与文案 |

> 面板脚本为 `XxxPanel.cs`（逻辑）+ `XxxPanel.Designer.cs`（代码生成绑定），Prefab 位于 `Assets/Res/Art/UIPrefab/`，文本全部使用 TextMeshPro。

### 5.7 配置资产（ScriptableObject）

| 资产 | 脚本 | 字段 |
| --- | --- | --- |
| `GameConfigDate.asset` | `GameConfig.cs` | 玩家初始属性：`MaxHp=3` `AttackDamage=1` `MoveSpeed=5` `AttackInterval=1` `AttackRadius=5` `WeaponCount=1`；无限刷怪：`SpawnInterval=3` `MaxAliveEnemies=10` `EnemyPowerPerSecond=0.02` `SurviveTimeToWin=120`(0=不设胜利) |
| `AbilityDatabase.asset` | `AbilityDatabase.cs` | `AllAbilities[]` 能力池引用集合 |
| 6 个能力资产 | `AbilityConfig.cs` | `AbilityName/Description/Icon/Effect/Value`；`AbilityEffect` 枚举：`AttackUp/SpeedUp/MaxHpUp/AttackSpeedUp/AttackRangeUp/MoreWeapon`（AttackAdd/AttackSpeedUp/MaxHpUp/MoreWeapon/SpeedAdd） |

> 资源加载契约：`GameConfigDate.asset` 与 `AbilityDatabase.asset` 必须标记 AB 名 **`data`**；预制体（Gem/Gold/Enemy/Weapon/Txt_Damage/Btn_Option）各按资源名标记 AB；UI 面板预制体在 **`uiprefab`** AB；场景 `gamestart_unity`/`maingame_unity`；音频 **`audio`**；字体 **`font`**。加载失败会打印 `LogKit.E` 提示"请确认已标记 AB"。

---

## 6. 依赖关系

### 6.1 Unity 包依赖（`Packages/manifest.json`）
- **微信小游戏转换**：`com.qq.weixin.minigame` @ gitee（SDK 0.1.1）
- **渲染**：`com.unity.render-pipelines.universal` 17.3.0（URP 2D）+ `com.unity.ugui` 2.0.0
- **输入**：`com.unity.inputsystem` 1.19.0（InputSystem Actions 驱动 + 虚拟摇杆 OnScreenControl）
- **2D**：animation 13.0.5 / aseprite 3.0.2 / psdimporter 12.0.2 / sprite 1.0.0 / spriteshape 13.0.0 / tilemap 1.0.0 + extras 6.0.2 / tooling 1.0.3
- **其他**：timeline / visualscripting / test-framework / collab-proxy / ide.rider / ide.visualstudio 等标准包
- 框架不依赖 UniRx/R3（QFramework 纯净版）。

### 6.2 架构注册依赖顺序
见 [2.2 启动时序](#22-架构初始化流程启动时序)。核心约束：
1. `GameModel` / `Storage` → `CloudSaveSystem`（OnInit 同步取 Model/Utility）
2. `CloudSaveSystem`（执行 `WX.cloud.Init`）→ `InviteSystem`（复用 Init、OnInit 注册 OnShow 并取 openid）
3. `InviteSystem` 内部依赖 `WXPlatformSystem.ShareToFriend`

### 6.3 运行时数据流
```
击敌人: PlayerController.TryAttackAllInRange
  → GameAssetsSystem.SpawnWeapon → Weapon.Init/Attack(动画事件)
  → Enemy.TakeDamage → Die → KillEnemyCommand → Model.AliveEnemies--
  → DropLoot → Gold/Gem(50/50) → Player 拾取
      Gold → PickupGoldCommand → Money++ → Register → Storage.SaveInt + CloudSaveSystem 防抖推云
      Gem  → PlayerGetGemCommand → Exp++ → LevelUpSystem 判定 → LevelUpEvent → PlayerController.OnLevelUp
        → AbilityPoolSystem.RollOptions(3) → GameLevelUpPanel → ChooseAbilityCommand → 修改属性即时生效

刷怪: WaveDriver.Update → WaveSystem.OnUpdate → SpawnEnemyCommand → 敌人出生登记 → EnemySpawnCommand
```

### 6.4 资源（AB）依赖
```
代码（Assets/Scripts + Assets/Res/Scripts）  ──ResLoader.Add2Load──▶  AB（data / prefabs / uiprefab / audio / font / gamestart_unity / maingame_unity）
UI 面板（UIKit.OpenPanelAsync） ────────────  uiprefab 包
场景加载（ResLoader.LoadSceneAsync） ───────  gamestart_unity / maingame_unity
```

### 6.5 微信平台依赖（真机运行）
```
Unity C#（WXPlatformSystem / CloudSaveSystem / InviteSystem）
    ├─ 微信 SDK（WeChatWASM.WX）：云开发 / 分享 / 授权 / 用户云存储 / OpenData
    ├─ 云函数（save/load/invite）→ 微信云开发数据库（save_data / invite_events / invite_rewards）
    ├─ 开放数据域（open-data/index.js）→ 好友关系链托管数据（key: bestTime）
    └─ 插件 UnityPlugin(wxe5a48f1ed5f544b7 v1.3.7) + 分包（wasmcode / data-package 并行预载）
```

---

## 7. 运行方式

### 7.1 编辑器直接运行（最快验证玩法）
1. 用 **Unity 6000.3.16f1** 打开工程（首次会还原包、编译）。
2. 打开场景 `Assets/Scenes/Boot.unity`，点击 ▶ Play（**必须从 Boot 场景进入**，它是唯一引导场景）。
3. 玩法验证：
   - 进入后 GameRoot 自动从 AB 异步加载 `GameStart` 场景（**若 AB 未构建，需先执行 7.2**）。
   - 使用键盘（InputSystem `Player/Move`：默认 WASD/方向键）或屏幕虚拟摇杆移动，角色自动攻击范围内敌人。
   - 存活 120 秒胜利 / 血量归零失败；金币与商店攻击跨局保留（PlayerPrefs）。
   - 编辑器下微信相关调用均为空操作：分享只打印标题、排行榜面板显示空白占位图（正常现象）。

### 7.2 构建 AssetBundle（运行前提）
AB 通过 QFramework ResKit 构建，菜单入口位于 ResKit 编辑器窗口（`Assets/QFramework/Toolkits/ResKit/Editor/`，`BuildScript.BuildAssetBundles`）：
- 资源预制体 / 场景 / 配置资产必须先勾选 **AssetBundle 名**（见 5.7 约定），构建后产物输出到 `Assets/StreamingAssets/AssetBundles/WebGL/` 与缓存目录 `AssetBundles/WebGL/`。
- 构建产物打包进 StreamingAssets，运行时由 ResKit 按 AB 名加载。

### 7.3 构建微信小游戏（目标平台）
版本锁定组合（**禁止中途升级**）：Unity 6000.3.16f1 + 微信 SDK 0.1.1 + 微信开发者工具 Stable 2.01 + URP 17.3.0。
1. 确保微信小程序后台已：注册 AppID（`wxd833d7def98f6c77` 待确认）、**开通「快适配」**、添加 **Unity 插件**（provider `wxe5a48f1ed5f544b7`）。具体 SOP 见 `docs/wechat-account-setup.md`。
2. Unity 中切平台为 **WebGL**（IL2CPP / Brotli），使用 `WXTemplate*` 微信模板构建。
3. 转换产出在 `WX/minigame/`（含 `game.json`：横屏、分包 `wasmcode`+`data-package`、插件与 workers 配置），构建原始目录为 `WX/webgl/`。
4. 用 **微信开发者工具** 导入 `WX/minigame/` 目录：
   - 本地调试：`python -m http.server 8080` 跑在 `minigame-out/webgl`，`game.js` 的 `DATA_CDN` 指向 `http://127.0.0.1:8080`（详见 `docs/engine-reference/unity/VERSION.md`）。
   - 真机调试验证微信云存档 / 排行榜 / 分享 / 邀请。
5. 打包发布：`minigame-out/minigame/` 为打包输出，正式上线需将数据包换为真实 CDN（腾讯云 COS / 阿里云 OSS）。

### 7.4 部署云函数（云存档 / 邀请有礼）
云函数源码在 `WX/custom/cloudfunctions/`（`load` / `save` / `invite` 三个 Node 函数，每目录含 `index.js` + `package.json`）：
1. 在微信开发者工具 / 小程序后台开通**云开发**，创建环境（与 `CloudSaveSystem.EnvId = cloud1-d8gevn4facad5c770` 一致）。
2. 分别上传部署 `load`、`save`、`invite` 三个云函数。
3. 数据库集合自动创建：`save_data`（云存档）、`invite_events`（邀请事件，幂等写入）、`invite_rewards`（领奖记录，防重复）。
4. 若目录被转换流程覆盖，用 `WX/restore-custom.bat` / `restore-custom.ps1` 恢复。

---

## 8. 全局约定与注意事项

### 8.1 平台约定（贯穿全项目）
- **WebGL 只能异步加载**：一切资源/场景/面板加载均用 `Add2Load + LoadAsync`、`OpenPanelAsync`、`LoadSceneAsync`。**同步 API 在微信真机首次加载必失败**。
- 微信能力全部用 `#if UNITY_WEBGL && !UNITY_EDITOR` 包裹，编辑器下为空操作；因此**编辑器测试不覆盖微信侧逻辑**，需要真机调试验证。
- 静态/常驻对象生命周期：`GameRoot` 常驻；`DamageTextPool` 随 MainGame 场景；`GameArchitecture`/System/Model/UIRoot 全局唯一。
- 云函数调用载荷（`SaveCallData` / `InviteCallData`）**字段不能为 null**，否则微信插件 `fixCallFunctionData` 递归 `Object.keys(null)` 崩溃。
- 微信 `ShareAppMessage` **拿不到成功回调**，"是否分享"只能记录"点击过分享"，资格判定一律以云端为准。

### 8.2 性能约定
- 高频攻击检测用**预分配缓冲**（`Collider2D[16]`）+ `ContactFilter2D`，避免每帧 GC。
- 飘字 / 武器走**对象池**；`DamageTextPool.Update` 用 `unscaledDeltaTime` 保证暂停（升级/结算 timeScale=0）时动画播完。
- 医生等级/经验等参数在 **GameConfig 资产**调整，勿硬编码。

### 8.3 常见坑位（代码注释中反复强调）
- 重开局必须显式调 `GameModel.ResetRunData()` + `IWaveSystem.ResetRun()`，否则上一局累计时间/强度/刷怪间隔残留。
- 武器对象池 `SafeObjectPool<Weapon>` 在场景卸载（MainGame 卸载）时 `Clear()`，防跨局失效引用。
- 面板析构阶段**不要**调 `UIKit.ClosePanel`（编辑器停止 Play 时 QF 惰性单例会在析构期重建 UIRoot，触发 "Some objects were not cleaned up" 警告）；面板关闭统一由场景加载时机处理。

### 8.4 相关文档索引
- `docs/architecture/m0-perf-baseline.md`：性能基线记录模板（编辑器/真机三档 FPS、GC Alloc 回填）
- `docs/engine-reference/unity/VERSION.md`：引擎与平台版本锁定（R7 实证）、本地 CDN 调试方法
- `docs/wechat-account-setup.md`：微信小游戏账号/AppID/快适配开通 SOP
- `.trae/documents/邀请分享奖励功能实施计划.md`：InviteSystem 实现设计文档
- `.trae/documents/TMP文字中文化及字体统一计划.md`：字体中文化计划