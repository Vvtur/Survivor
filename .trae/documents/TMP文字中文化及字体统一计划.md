# 计划：TMP 文字全部中文化 + 统一使用中文字体（FZSTK SDF）

## 一、现状分析（基于实际文件审计）

### 1. 字体使用情况
项目中存在两个 TMP 字体资产：

| 字体资产 | GUID | 是否支持中文 | 使用位置 |
| --- | --- | --- | --- |
| `Assets/Res/Art/Font/FZSTK SDF.asset`（方正舒体，动态字体 `m_AtlasPopulationMode:1`，运行时可动态烘焙任意字符） | `1e0457e1dfbb35f4f9f571fab1c79b56` | 是 | 绝大多数 UI 预制体 + Boot 场景 |
| `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`（TMP 默认字体，仅英文/拉丁字符集） | `8f586378b4e144a9851e7b34d9b748ee` | 否 | **仅 `InvitePanel.prefab` 的 3 个 TMP 组件** |

结论：目前只有邀请面板（InvitePanel）误用了默认字体（显示不了中文），其余 UI 已全部引用 FZSTK SDF。

### 2. 文本分布（全部 TMP 文本来源）
- **UI 预制体 `m_text` 字段**：8 个预制体（GameOverPanel、InvitePanel、LeaderboardPanel、GameStartPanel、PlayerInfoPanel、ShopPanel、Cotent/Btn_Option、Cotent/Txt_Damage）
- **C# 代码运行时赋值**：`PlayerInfoPanel.cs`（HP/EXP/LV/Attack）、`ShopPanel.cs`（Gold）
- **能力配置资产**（`Assets/Res/GameRes/Data/*.asset`）：AttackAdd / SpeedAdd / MaxHpUp / AttackSpeedUp / MoreWeapon 的 `AbilityName`/`Description` 全为英文
- **Boot 场景**：已是中文"初始化中"，字体已是 FZSTK SDF，**无需改动**

## 二、变更清单

### 1. 字体统一（仅 InvitePanel.prefab）
将 3 个 TMP 组件（Share、Close、Claim）从默认字体 LiberationSans SDF 改为 FZSTK SDF，需同时改两行：
- `m_fontAsset: {fileID: 11400000, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}` → `m_fontAsset: {fileID: 11400000, guid: 1e0457e1dfbb35f4f9f571fab1c79b56, type: 2}`（3 处：484/621/760 行）
- `m_sharedMaterial: {fileID: 2180264, guid: 8f586378b4e144a9851e7b34d9b748ee, type: 2}` → `m_sharedMaterial: {fileID: -9186138327807689033, guid: 1e0457e1dfbb35f4f9f571fab1c79b56, type: 2}`（3 处：485/622/761 行）

### 2. 预制体文字翻译（m_text 字段）
| 文件 | 原文本 | 新文本 |
| --- | --- | --- |
| `UIPrefab/GameOverPanel.prefab` | `GameOver` | `游戏结束` |
| `UIPrefab/GameOverPanel.prefab` | `Button` | `重新开始` |
| `UIPrefab/InvitePanel.prefab` | `Share` | `分享给好友` |
| `UIPrefab/InvitePanel.prefab` | `Close` | `关闭` |
| `UIPrefab/InvitePanel.prefab` | `Claim\n\n`（保留尾部换行） | `领取奖励\n\n` |
| `UIPrefab/InvitePanel.prefab` | `New Text`（Txt_Tip，运行时会覆盖） | `分享给好友，一起领金币！` |
| `UIPrefab/LeaderboardPanel.prefab` | `Share` | `分享` |
| `UIPrefab/GameStartPanel.prefab` | `Start` | `开始游戏` |
| `UIPrefab/GameStartPanel.prefab` | `Rank` | `排行榜` |
| `UIPrefab/GameStartPanel.prefab` | `Shop` | `商店` |
| `UIPrefab/GameStartPanel.prefab` | `Invite` | `邀请有礼` |
| `UIPrefab/PlayerInfoPanel.prefab`（Txt_HP） | `New Text` | `生命: 0/0` |
| `UIPrefab/PlayerInfoPanel.prefab`（Txt_Lv） | `New Text` | `等级: 0` |
| `UIPrefab/PlayerInfoPanel.prefab`（Txt_Exp） | `New Text` | `经验: 0/1` |
| `UIPrefab/PlayerInfoPanel.prefab`（Txt_Attack） | `New Text` | `攻击: 0` |
| `UIPrefab/ShopPanel.prefab` | `AttackAdd \nCost 1` | `攻击+1\n消耗 1 金币` |
| `UIPrefab/ShopPanel.prefab` | `Close` | `关闭` |
| `UIPrefab/ShopPanel.prefab` | `Shop` | `商店` |
| `UIPrefab/Cotent/Btn_Option.prefab` | `Button`（运行时会覆盖） | `选择能力` |
| `UIPrefab/Cotent/Txt_Damage.prefab` | `1`（伤害数字） | 不变 |

### 3. 代码字符串翻译
- `Assets/Res/Scripts/UI/PlayerInfoPanel.cs`：
  - `$"HP: {...}"` → `$"生命: {...}"`
  - `$"EXP: {...}"` → `$"经验: {...}"`
  - `$"LV: {...}"` → `$"等级: {...}"`
  - `$"Attack: {...}"` → `$"攻击: {...}"`
- `Assets/Res/Scripts/UI/ShopPanel.cs`：`"Gold:" + ...` → `"金币:" + ...`

### 4. 能力配置资产翻译（AbilityName / Description）
| 文件 | 能力名 | 描述 |
| --- | --- | --- |
| `Data/AttackAdd.asset` | 攻击强化 | 攻击力 +1 |
| `Data/SpeedAdd.asset` | 移速提升 | 移动速度 +1 |
| `Data/MaxHpUp.asset` | 生命强化 | 最大生命 +1 |
| `Data/AttackSpeedUp.asset` | 攻速提升 | 攻击速度提升 |
| `Data/MoreWeapon.asset` | 双持武器 | 同时装备两把武器 |

## 三、假设与决策
- 翻译为自然的中文游戏文案，保留原意的同时贴近手游习惯用语。
- `InvitePanel` 的 `Claim` 文本保留尾部换行，避免破坏按钮内文字垂直布局。
- 不修改微信开放数据域（`WX/minigame/open-data/index.js`）里的画布文字——那是 canvas 绘制，不属于 TMP 范畴；如需要可另行处理。
- FZSTK SDF 为动态字体，运行时会自动为新增中文字符烘焙图集，因此翻译后新增的中文都能正常显示，无需重建字体资产。

## 四、验证步骤
1. 在 Unity 编辑器中打开各 UI 预制体，确认：
   - 所有 TMP 组件 `Font Asset` 均为 `FZSTK SDF`（尤其 InvitePanel 的 3 个）；
   - 中文文字正常显示、无方框/缺字；
   - 各按钮/标题文字内容为中文且布局未变形。
2. 运行游戏（编辑器）检查：开始界面四按钮中文、商店文案、玩家信息 HUD（生命/经验/等级/攻击）、升级三选一（能力名+描述）、游戏结束面板、邀请面板。
3. 该工程使用 AssetBundle：修改预制体与能力资产后需重新 Build AssetBundle（font / uiprefab / data 相关包），并重新走 Unity→微信转换流程（涉及 WX 目录，需用 `WX/restore-custom.bat` 恢复自定义文件）。
4. 微信开发者工具中真机/模拟器预览，确认中文字体正常渲染。
