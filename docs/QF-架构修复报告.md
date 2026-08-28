# Q. Framework 架构合规性修复报告

> **项目**：Survivor（Unity + QFramework v1.x）
> **修复日期**：本次会话
> **修复依据**：[QFramework 官方文档 - 架构规范与推荐用法](https://qf.readthedocs.io/zh-cn/latest/QFramework_v1.0_Guide/02_architecture/10_architecture_spec_and_recommended_usage.html)

---

## 一、修复概览

| # | 严重度 | 问题 | 修复方式 | 涉及文件 |
|---|--------|------|----------|----------|
| 1 | 🔴 高 | `WaveSystem`（ISystem）越层 `SendCommand`，违反分层规范 | 改为 `SendEvent` + `WaveDriver`（IController）订阅事件后 `SendCommand` | `WaveSystem.cs` / `WaveDriver.cs` / `GameEvent.cs` |
| 2 | 🔴 高 | `PlayerInfoPanel` 重复订阅 + 订阅永不释放（内存泄漏） | 整理为单一 `Awake` 入口 + Unity `OnDestroy` 注销 | `PlayerInfoPanel.cs` |
| 3 | 🟡 中 | `BuyCommand` 字段命名不符合 QF 规范（无 `m` 前缀） | 改为 `mCost` + 完善注释 | `BuyCommand.cs` |
| 4 | 🟢 低 | 空壳 `IGameManagerSystem` / `GameManagerSystem`（无人引用） | 整文件移除 | `GameManagerSystem.cs` / `GameArchitecture.cs` |
| 5 | 🟢 低 | 残留注释指向已删除的 `GameManagerSystem` | 改为 `WaveSystem` | `PlayerController.cs` / `GameEvent.cs` |
| 6 | 🟢 低 | 多余的 `using QFramework.UI;` | 移除 | `GameArchitecture.cs` |

**未修改（按你指示保留）**：
- ❌ UI Panel 全部保持 `GameArchitecture.Interface.GetXxx` 静态访问风格，不统一为 `this.GetXxx`（原因：见下文 §四）
- ❌ 不引入 Query（项目简单，目前无查询需求）
- ❌ UI namespace 保持 `QFramework.UI`（与 `Res/Scripts/UI/` 配套使用 CodeGenKit）

---

## 二、修复详情

### 修复 1：WaveSystem 越层 SendCommand（最严重的规范违规）

#### 规范原文
> **ISystem** 可获取：System、Model、Utility；可发送：**Event**。
> **IController 更改 ISystem、IModel 的状态必须用 Command**。
> 上层向下层通信用方法调用（状态变更用 Command）；下层向上层通信用事件。
>
> （QFramework `ISystem` 接口定义不包含 `ICanSendCommand`，而 `IController` 包含）

#### 修复前 ❌
```csharp
// Scripts/Game/Systems/WaveSystem.cs
private void SpawnEnemy()
{
    var power = 1f + mElapsedTime * mModel.EnemyPowerPerSecond.Value;
    GameArchitecture.Interface.SendCommand(new SpawnEnemyCommand(GetSpawnPosition(), power));
    //                                  ^^^^^^^^^^^^ System 层调用了 Command，违反分层
}
```

#### 修复后 ✅
**1）新增事件**（`Scripts/Game/GameEvent.cs`）：
```csharp
/// <summary>
/// 敌人生成请求（由 WaveSystem 发出，WaveDriver 接收后通过 SpawnEnemyCommand 执行）。
/// 规范：System 不能 SendCommand，需通过事件通知 IController 层来执行状态变更。
/// </summary>
public struct SpawnEnemyRequestEvent
{
    public UnityEngine.Vector3 SpawnPosition;
    public float Power;   // 强度系数（≥1，随时间递增）
}
```

**2）WaveSystem 改为 SendEvent**（`Scripts/Game/Systems/WaveSystem.cs`）：
```csharp
private void SpawnEnemy()
{
    var power = 1f + mElapsedTime * mModel.EnemyPowerPerSecond.Value;
    this.SendEvent(new SpawnEnemyRequestEvent
    {
        SpawnPosition = GetSpawnPosition(),
        Power = power,
    });
}
```

**3）WaveDriver（IController）订阅事件并 SendCommand**（`Scripts/Gameplay/WaveDriver.cs`）：
```csharp
private void Awake()
{
    mWaveSystem = this.GetSystem<IWaveSystem>();

    // 订阅 WaveSystem 发出的"敌人生成请求"事件，转发为 Command
    // UnRegisterWhenGameObjectDestroyed：场景销毁时自动注销，避免内存泄漏
    this.RegisterEvent<SpawnEnemyRequestEvent>(OnSpawnEnemyRequest)
        .UnRegisterWhenGameObjectDestroyed(this);
}

private void OnSpawnEnemyRequest(SpawnEnemyRequestEvent e)
{
    this.SendCommand(new SpawnEnemyCommand(e.SpawnPosition, e.Power));
}
```

**数据流（修复后符合规范）**：
```
WaveSystem (ISystem) 
    │  SendEvent: SpawnEnemyRequestEvent
    ▼
事件总线
    │  RegisterEvent: SpawnEnemyRequestEvent
    ▼
WaveDriver (IController) 
    │  SendCommand: SpawnEnemyCommand
    ▼
SpawnEnemyCommand (ICommand) ── 修改 Model + 创建敌人
```

#### 行为变化
- ✅ 无 — 敌人依然每 `spawnInterval` 秒生成一次
- ✅ 修复了架构分层违规
- ✅ 场景销毁时 WaveDriver 的事件订阅通过 `UnRegisterWhenGameObjectDestroyed` 自动注销

---

### 修复 2：PlayerInfoPanel 重复订阅 + 内存泄漏

#### 修复前 ❌
```csharp
// Assets/Res/Scripts/UI/PlayerInfoPanel.cs
public partial class PlayerInfoPanel : UIPanel, IUnRegisterList
{
    // ...（字段定义）

    private void Awake()
    {
        mModel = GameArchitecture.Interface.GetModel<GameModel>();
        mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
        // ... 4 个 Register
    }

    protected override void OnInit(IUIData uiData = null)  // ← 这方法不会被 QF 调用
    {
        mData = uiData as PlayerInfoPanelData ?? new PlayerInfoPanelData();
        mModel = GameArchitecture.Interface.GetModel<GameModel>();
        mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
        // ... 同样的 4 个 Register ← 死代码
    }

    protected override void OnDestroy()  // ← 这是 QF 的 protected virtual，不会被 Unity 调用
    {
        base.OnDestroy();
        this.UnRegisterAll();  // ← 永远不会被触发 → 内存泄漏
    }
}
```

**问题分析**：
1. `PlayerInfoPanel` 挂在 `PlayerInfoPanel.prefab` 上随 `MainGame` 场景加载，**不**通过 `UIKit.OpenPanel<PlayerInfoPanel>()` 打开（搜遍全工程没找到调用点）
2. `UIPanel.OnInit` / `OnOpen` / `OnClose` / `OnDestroy`（QF 虚方法）只在 `UIKit.OpenPanel` 内部被调用，本面板这些回调**永远不触发**
3. 结果：`OnInit` 里的 5 个 `Register` 是死代码；`OnDestroy` 里的 `UnRegisterAll` 永远不执行，BindableProperty 一直强引用面板对象 → 内存泄漏
4. 实际生效的只有 `Awake` 里的 5 个 `Register`（单次），功能正常但**注销路径缺失**

#### 修复后 ✅
```csharp
// Assets/Res/Scripts/UI/PlayerInfoPanel.cs
public partial class PlayerInfoPanel : UIPanel, IUnRegisterList
{
    public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();
    private GameModel mModel;
    private bool mSubscribed;

    private void Awake()
    {
        // 场景加载时挂载本面板：订阅 Model 全部需要展示的字段
        // 注意：本面板**不走 UIKit 生命周期**（不在 UIKit 注册的 Panel 列表里），
        // QF 的 UIPanel.OnInit 不会被调用，所有初始化逻辑放这里。
        SubscribeModel();
    }

    private void SubscribeModel()
    {
        if (mSubscribed) return;   // 防御性：Unity 不会重入 Awake，但保留更稳
        mSubscribed = true;

        mModel = GameArchitecture.Interface.GetModel<GameModel>();

        mModel.HP.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
        mModel.MaxHp.RegisterWithInitValue(_ => RefreshHP()).AddToUnregisterList(this);
        mModel.Exp.RegisterWithInitValue(_ => RefreshExp()).AddToUnregisterList(this);
        mModel.Level.RegisterWithInitValue(_ => RefreshLevel()).AddToUnregisterList(this);
        mModel.AttackDamage.RegisterWithInitValue(_ => RefreshAttack()).AddToUnregisterList(this);
    }

    // Unity 的 OnDestroy 消息：场景销毁（切到 GameStart）或编辑器停止时触发。
    // 必须手动注销所有 BindableProperty 订阅，否则面板虽然销毁了，
    // GameModel 里的 Register 列表还强引用着回调 → 内存泄漏。
    // 注：UIPanel 也定义了 protected virtual void OnDestroy()（QF 生命周期，非 Unity 消息），
    // 本面板不走 QF 生命周期，这里用 new 关键字隐藏基类方法，避免 CS0108 警告。
    // 不调 base.OnDestroy() 的原因：ClearUIComponents 只为复用场景清理 SerializeField，
    // 本面板每次随场景加载新建，不需要复用，无需清理。
    private new void OnDestroy()
    {
        this.UnRegisterAll();
    }

    // ---- 以下是 UIPanel 虚方法（保留以满足继承约束） ----
    // 本面板不走 UIKit 生命周期，以下方法**不会被 QF 调用**，留空实现即可。
    protected override void OnInit(IUIData uiData = null) { }
    protected override void OnOpen(IUIData uiData = null) { }
    protected override void OnShow() { }
    protected override void OnHide() { }
    protected override void OnClose() { }
}
```

#### 行为变化
- ✅ 单一入口（`Awake` → `SubscribeModel`），重复订阅风险消除
- ✅ `OnDestroy`（Unity 消息，私有 + `new` 关键字）场景销毁时自动注销所有订阅
- ✅ `mSubscribed` 标志位防御性保护（Unity 不会重入 Awake，但保留更稳）
- ✅ QF 虚方法 `OnInit/OnOpen/OnShow/OnHide/OnClose` 留空占位（满足 abstract 约束），不会被 QF 调用

---

### 修复 3：BuyCommand 字段命名规范化

#### 修复前 ❌
```csharp
public class BuyCommand : AbstractCommand
{
    int cost = 0;  // ← 字段名无 m 前缀
    public BuyCommand(int cost) { this.cost = cost; }
    protected override void OnExecute()
    {
        var model = this.GetModel<GameModel>();
        if (model.Money.Value >= cost) { ... }
    }
}
```

#### 修复后 ✅
```csharp
public class BuyCommand : AbstractCommand
{
    private readonly int mCost;   // ← QF 命名规范：m 前缀

    public BuyCommand(int cost) { mCost = cost; }

    protected override void OnExecute()
    {
        var model = this.GetModel<GameModel>();
        if (model.Money.Value < mCost) return;   // 早返回，代码更清晰
        model.Money.Value -= mCost;
        model.Attack.Value += 1;
    }
}
```

#### 行为变化
- ✅ 字段命名符合 QF 规范
- ✅ 业务逻辑不变（购买 1 金币扣 1 攻击 +1）
- ⚠️ 旧代码有 `if (model.Money.Value >= cost) { ... } else return;` 三段式，改为 `if (... < mCost) return;` 早返回，更扁平

---

### 修复 4：删除空壳 `IGameManagerSystem` / `GameManagerSystem`

#### 原因
- 原 `GameManagerSystem.OnInit` 是空方法，胜利逻辑已经搬到 `WaveSystem`
- `IGameManagerSystem` 接口全工程**无人引用**（grep 验证过）
- 注册它会在 `Architecture.Init` 时多走一次空 `OnInit` 流程，浪费启动时间

#### 修复
- `Scripts/Game/Systems/GameManagerSystem.cs` — 整文件重写为只剩 `ILevelUpSystem` / `LevelUpSystem`
- `Scripts/Game/GameArchitecture.cs` — 删除 `RegisterSystem<IGameManagerSystem>(...)` 一行

#### 行为变化
- ✅ 架构启动少一个空 `OnInit` 调用
- ✅ 行为完全不变（因为 `GameManagerSystem` 本来就什么都不做）

---

### 修复 5：注释清理

`Scripts/Game/GameEvent.cs`：
```csharp
- /// <summary>游戏胜利事件（由 GameManagerSystem 发出）</summary>
+ /// <summary>游戏胜利事件（由 WaveSystem 发出，存活时间到触发）</summary>
```

`Scripts/Gameplay/PlayerController.cs:148`：
```csharp
- // 游戏胜利：由 GameManagerSystem 发送 GameWinEvent 触发
+ // 游戏胜利：由 WaveSystem 发送 GameWinEvent 触发
```

---

### 修复 6：清理多余 using

`Scripts/Game/GameArchitecture.cs`：
```csharp
- using QFramework;
- using QFramework.UI;  // ← 原本是给 IGameManagerSystem 用的，删 System 后不再需要
+ using QFramework;
```

---

## 三、未修复事项（保留原样）

按你指示，下列事项**不修改**：

### 1. UI 面板保持 `GameArchitecture.Interface.GetXxx` 静态访问
最初尝试将 UI 中的 `GameArchitecture.Interface.GetSystem<>()` 改为 `this.GetSystem<>()`（更"QF 风格"），但发现：

- `UIPanel` 基类只实现 `IPanel`，**不**实现 `IController` / `IBelongToArchitecture`
- `this.GetSystem<T>()` 等扩展方法要求 `this` 实现 `ICanGetSystem`（继承自 `IBelongToArchitecture`），**否则编译失败**
- `partial class AbilityOptionItem` 在 Designer.cs 显式声明 `: IController`（所以它可以用 `this.GetXxx`），但其它 panel 的 Designer.cs 都没声明
- 结论：原代码 `GameArchitecture.Interface.GetXxx` 静态访问**是正确选择**（绕开接口要求），改回会编译失败

**所以这一项不修改，**只在所有 UI 面板的 `OnOpen` / `OnInit` / `OnClick` 等处的 `GameArchitecture.Interface.GetXxx` 上方加了注释说明原因：

```csharp
// 注：UIPanel 基类不实现 IController，无法用 this.GetSystem<> 扩展方法，
// 只能通过 GameArchitecture.Interface 静态单例访问
GameArchitecture.Interface.GetSystem<IInviteSystem>().TrackInviteFromLaunch();
```

### 2. 不引入 Query
项目目前没有"跨层查询"场景，所有数据访问都是 `this.GetModel<GameModel>().XXX` 直读直用。强行引入 `IQuery<TResult>` 反而增加模板代码。**等真正有需要时再引入。**

### 3. UI namespace 保持 `QFramework.UI`
`Res/Scripts/UI/` 下的所有 panel 用 `QFramework.UI` 命名空间，是 QF 推荐做法（与 `Res/` 目录 + CodeGenKit 配套）。改成 `QFramework.Gameplay` 反而会破坏 CodeGenKit 的代码生成路径。

---

## 四、修改文件清单

```
Assets/Res/Scripts/UI/GameLevelUpPanel.cs    注释（说明 GameArchitecture.Interface 是正确选择）
Assets/Res/Scripts/UI/GameStartPanel.cs      注释（同上）
Assets/Res/Scripts/UI/InvitePanel.cs         注释（同上）
Assets/Res/Scripts/UI/LeaderboardPanel.cs    注释（同上）
Assets/Res/Scripts/UI/PlayerInfoPanel.cs     ⭐ 整理：单入口 + Unity OnDestroy 注销
Assets/Res/Scripts/UI/ShopPanel.cs           注释（同上）
Assets/Scripts/Game/Commands/BuyCommand.cs   ⭐ 字段命名 mCost + 早返回重构
Assets/Scripts/Game/GameArchitecture.cs      ⭐ 删除空 IGameManagerSystem 注册 + 移除多余 using
Assets/Scripts/Game/GameEvent.cs             ⭐ 新增 SpawnEnemyRequestEvent + 注释修正
Assets/Scripts/Game/Systems/GameManagerSystem.cs  ⭐ 整文件重写（删除空 IGameManagerSystem）
Assets/Scripts/Game/Systems/WaveSystem.cs    ⭐ SendCommand 改为 SendEvent
Assets/Scripts/Gameplay/PlayerController.cs  注释（指向 WaveSystem）
Assets/Scripts/Gameplay/WaveDriver.cs        ⭐ 新增：订阅 SpawnEnemyRequestEvent + 转发为 Command
```

13 个文件变更，3 个核心修复（⭐ 标记）。

---

## 五、风险与回滚

### 编译风险
- **WaveSystem 改动**：用到了 `ISystem` 接口的 `SendEvent<T>`（合规）。`WaveDriver` 显式 `IController`，所有 `this.GetXxx` 合法
- **PlayerInfoPanel 改动**：`private new void OnDestroy()` 用 `new` 关键字隐藏 `UIPanel.OnDestroy` 虚方法，避免 CS0108 警告
- **UI 注释改动**：纯注释，不影响代码
- **BuyCommand 改动**：早返回替代三段式 if/else，逻辑等价

### 运行风险
- **WaveDriver.Awake 顺序**：必须在 `WaveSystem` 首次 `SendEvent` 之前订阅事件
  - `WaveSystem.OnInit` 是在 `GameArchitecture.Interface` 被访问时同步执行的
  - `WaveDriver` 是场景 MonoBehaviour，`Awake` 在场景加载时执行
  - `GameRoot.Boot()` 顺序：1) `ResKit.InitAsync()` → 2) `_ = GameArchitecture.Interface`（触发 WaveSystem.OnInit 注册）→ 3) `LoadSceneAsync("MainGame")` → 4) `MainGame` 加载时 `WaveDriver.Awake` 订阅
  - **风险点**：如果 `WaveSystem` 在 `WaveDriver` 还没订阅时 `SendEvent` 怎么办？
  - **不会发生**：`WaveSystem.OnInit` 只缓存了 `mModel`、调用了 `ResetRun()`，**没有立刻发事件**。真正发事件是在 `OnUpdate()` 里（每帧调用），而 `WaveDriver.Awake` 在 `MainGame` 场景第一帧前就完成了
- **PlayerInfoPanel 订阅时机**：原版 `Awake` 里就在订阅（行为不变）
- **BuyCommand 早返回**：边界条件 `model.Money.Value < mCost` 旧版会进 else 分支返回，新版直接 `return`，**等价**

### 回滚方法
```bash
git checkout HEAD -- Assets/Scripts/Game/ Assets/Res/Scripts/UI/ Assets/Scripts/Gameplay/
```

---

## 六、修复前后规范对照

| QF 规范条目 | 修复前 | 修复后 |
|---|---|---|
| ISystem 不能 SendCommand | ❌ WaveSystem 违规 | ✅ 改为 SendEvent |
| ICommand 字段命名 mXxx | ⚠️ BuyCommand 用 `cost` | ✅ 改为 `mCost` |
| 死代码 / 空壳代码 | ⚠️ GameManagerSystem 是空壳 | ✅ 删除 |
| 内存泄漏防护 | ❌ PlayerInfoPanel 不注销 | ✅ Unity OnDestroy 注销 |
| 单一入口初始化 | ❌ PlayerInfoPanel 双订阅 | ✅ 单一 Awake 入口 |
| 注释与代码一致 | ⚠️ 注释指向已删除的 System | ✅ 全部更新 |
| 多余 using | ⚠️ GameArchitecture 多了 QFramework.UI | ✅ 清理 |

---

## 七、后续建议（不实现，仅备忘）

1. **引入 `IQuery` 模式**：当出现"复杂条件查询"（如 `GetStrongestEnemyQuery`）时使用，可避免在多处直接遍历 `mModel.AliveEnemies`
2. **把 `UI 面板` 也实现 `IController`**：在 Designer.cs 把 `partial class XxxPanel` 显式声明为 `: IController`，提供 `GetArchitecture()`，就可以把 `GameArchitecture.Interface.GetXxx` 统一为 `this.GetXxx` —— 这需要在所有 panel 的 Designer.cs 加代码
3. **`WaveSystem.ResetRun()` 集中化**：目前在 `PlayerController.Start` 显式调用；更 QF 风格的做法是监听 `OnEnterMainGame` 架构级事件，由所有 System 统一处理"局内状态重置"
4. **`ChooseAbilityCommand` 拆分为多个 Command**：每个 `AbilityEffect` 一个 Command，能力配置里只放 `Command` 的 Type。这样新增能力只需添加 Command 类 + 配置项，不改 switch 代码

---

*报告结束*
