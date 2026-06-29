# TopDownSurvivalDemo 技术分析与实现文档

## 1. 项目概述

`TopDownSurvivalDemo` 是一个 Unity 2D 俯视角生存类游戏 Demo。当前项目已经实现了从开始界面、选角界面、游戏场景、刷怪战斗、关卡结算、升级强化、商店遗物、Boss 关卡到存档继续游戏的一条基础闭环。

项目资源主要通过 `Resources` 动态加载。业务代码集中在 `Assets/_Project/Scripts/Game`。业务层复用了框架提供的 UI 管理、场景管理、对象池、音频和计时器能力。

## 2. 目录结构

```text
Assets/_Project/Scripts/Game
├── Camera      摄像机跟随与边界限制
├── Combat      玩家攻击、弹体、伤害计算、强化选项
├── Config      角色、怪物、遗物、关卡刷怪配置读取
├── Data        玩家运行时数据、选角数据、存档数据
├── Monster     小怪、Boss、刷怪器
├── Pickup      金币掉落与吸附拾取
├── Player      玩家移动、战斗、等级经验金币
├── Relic       遗物配置、遗物效果处理
├── Scene       游戏场景启动、关卡流程、奖励流程
└── UI          开始、选角、游戏、结算、商店等面板
```

配置文件位于：

```text
Assets/_Project/Resources/Configs
├── characters.json      角色初始属性
├── monsters.json        小怪和 Boss 属性
├── relics.json          遗物配置
└── stage_spawns.json    关卡刷怪权重配置
```

## 3. 总体架构

项目采用“框架层 + 业务层”的结构。

框架层负责通用能力：

- `UIMgr`：UI 面板加载、显示、隐藏。
- `SceneMgr`：场景切换。
- `PoolMgr`：对象池管理，用于小怪、金币、预警、弹体等。
- `MusicMgr`：背景音乐和音效播放。
- `TimerMgr`：倒计时和定时任务。

业务层负责游戏规则：

- `GameSceneStarter` 创建游戏场景运行时对象。
- `GameLevelManager` 控制关卡开始、倒计时、结算和进入下一关。
- `MonsterSpawner` 根据关卡配置生成怪物。
- `PlayerController2D` 控制玩家移动、动画和受击死亡。
- `PlayerCombat` 根据角色攻击类型委托给近战或远程攻击策略。
- `PlayerRelicEffects` 保存玩家拥有的遗物效果，并参与伤害、回血、复活等逻辑。

## 4. 场景流程

### 4.1 开始界面

入口脚本为 `BeginSceneStarter` 和 `BeginPanel`。

流程：

1. 加载背景音乐。
2. 动态加载 `Canvas`、`EventSystem` 和 `BeginPanel`。
3. 点击“开始游戏”进入选角场景。
4. 点击“继续游戏”时，如果存在 json 存档，则加载游戏场景并恢复存档。
5. 如果没有存档，则弹出提示面板，1 秒后自动关闭。
6. 点击“退出游戏”调用退出逻辑。

### 4.2 选角界面

入口脚本为 `ChooseCharacterPanel`。

主要职责：

- 从 `characters.json` 读取角色配置。
- 左右切换角色。
- 在 `characterpos` 位置生成角色预览。
- 刷新角色属性文本。
- 点击开始后保存选中角色 id，并进入 `GameScene`。

### 4.3 游戏场景

入口脚本为 `GameSceneStarter`。

主要职责：

- 判断是否需要读取存档。
- 根据选中角色或存档角色生成玩家。
- 初始化玩家移动、战斗、成长、遗物组件。
- 初始化摄像机跟随。
- 初始化刷怪器和关卡管理器。
- 动态加载 `GamePanel` 并绑定玩家数据。

## 5. 玩家系统

### 5.1 玩家运行时数据

`PlayerRuntimeData` 保存玩家当前战斗属性：

- 角色 id
- 名称
- 攻击类型
- 最大生命
- 当前生命
- 攻击
- 防御
- 移速
- 暴击率

这些数据由 `characters.json` 初始化，之后会受到升级强化、遗物、存档恢复影响。

### 5.2 玩家移动和动画

`PlayerController2D` 负责：

- WASD 移动。
- 根据地图边界限制玩家位置。
- 更新角色朝向。
- 播放出生、移动、攻击、受击、死亡动画。
- 玩家死亡时通知关卡管理器。

摄像机由 `CameraFollow2D` 跟随玩家，并限制画面不超出地图范围。

### 5.3 玩家战斗

`PlayerCombat` 按角色攻击类型选择攻击策略：

- `MeleeAttackStrategy`：战士近战扇形攻击。
- `RangedAttackStrategy`：弓手远程自动索敌射箭。

伤害计算由 `DamageUtil` 统一处理：

基础伤害 = max(1, 攻击 - 防御)
暴击后 = 基础伤害 * 暴击倍率
最终伤害 = 暴击后伤害 * 其他伤害倍率

暴击显示和普通伤害显示使用不同颜色。

## 6. 怪物与 Boss 系统

### 6.1 小怪

`MonsterController` 负责小怪行为：

- 自动索敌。
- 追击玩家。
- 远程怪发射火球。
- 接触玩家造成伤害。
- 受伤显示伤害数字。
- 死亡后掉落金币和经验。
- 死亡淡出后回收到对象池。

小怪属性来自 `monsters.json`。

### 6.2 刷怪器

`MonsterSpawner` 负责刷怪：

1. 根据当前关卡读取 `stage_spawns.json`。
2. 在地图内随机选择刷怪点。
3. 先生成预警预制体。
4. 预警结束后从对象池取出怪物。
5. 场上怪物超过上限时暂停生成。

刷怪权重已经配置化。例如：

```json
{
  "stage": 3,
  "spawnCount": 3,
  "spawnInterval": 4,
  "monsters": [
    { "monsterId": "monster_1", "weight": 2 },
    { "monsterId": "monster_2", "weight": 2 },
    { "monsterId": "monster_3", "weight": 1 }
  ]
}
```

### 6.3 Boss

Boss 由 `BossController` 控制，生成逻辑由 `GameBossStageService` 管理。

当前规则：

- 第 5 关生成 Boss1。
- 第 10 关生成 Boss2。
- Boss 血条通过 `GamePanel` 的 `BossHp` 绑定。

Boss1：

- 面向方向扇形攻击。
- 半血时进入免伤施法，预警后召唤 9 只小怪。

Boss2：

- 玩家进入攻击范围后冲撞。
- 半血时进入免伤施法，随机位置显示火焰预警，再生成火焰区域。

## 7. 关卡系统

`GameLevelManager` 是关卡主控。

职责：

- 控制当前关卡编号。
- 设置每关倒计时。
- 关卡开始时回满玩家生命。
- 根据关卡类型启动普通刷怪或 Boss 关。
- 关卡开始时保存存档快照。
- 玩家死亡时结算失败。
- 倒计时结束且玩家存活时通关。
- Boss 被击杀时立即通关，并发放奖励。

当前 10 关时长：

45, 45, 45, 45, 60, 45, 45, 45, 45, 60

第 5、10 关为 Boss 关。

## 8. 升级、强化与商店

### 8.1 等级和经验

`PlayerProgression` 管理：

- 等级
- 当前经验
- 升级所需经验
- 金币

怪物死亡时给玩家经验。经验达到需求后自动升级，多余经验保留。

### 8.2 关后强化

`GameRewardFlow` 负责关后流程。

如果玩家本关升级：

1. 播放奖励动画。
2. 弹出 `ChooseBuffPanel`。
3. 每升一级进行一次三选一强化。
4. 强化次数用完后进入下一步。

如果没有升级，则跳过强化。

### 8.3 商店遗物

第 4、9 关结束后进入商店。

`ShopPanel` 负责：

- 随机生成 6 个遗物槽位。
- 按普通、罕见、稀有比例分配。
- 支持多选购买。
- 钱够则一起购买，钱不够则显示提示。
- 购买后应用遗物效果并刷新玩家属性。

遗物配置来自 `relics.json`。

## 9. 遗物系统

遗物系统由三部分组成：

- `RelicConfig`：遗物配置结构。
- `RelicConfigLoader`：从 json 加载遗物。
- `RelicEffectHandlers`：不同遗物效果的处理器。

当前采用“效果类型字符串 + 处理器注册表”的方式扩展。新增遗物时通常只需要：

1. 在 `relics.json` 中添加配置。
2. 如果是已有效果类型，则无需改代码。
3. 如果是新效果类型，则新增一个 `IRelicEffectHandler` 实现，并在注册表中注册。

当前支持的效果包括：

- 属性增加：攻击、防御、生命、移速、暴击。
- 周期回血。
- 获得金币。
- 暴击伤害倍率。
- 移速转防御。
- 弓手三箭分裂。
- 暴击吸血。
- 属性重排。
- 一次性复活。
- 普通吸血。
- 每秒扣血。
- Boss 伤害倍率。

## 10. 对象池使用

项目中大量临时对象使用对象池：

- 小怪
- 金币
- 伤害数字
- 刷怪预警
- 箭矢
- 火球

优势：

- 减少频繁 `Instantiate` / `Destroy`。
- 降低运行时 GC。
- 适合生存类游戏大量怪物和掉落物的场景。

注意事项：

- 被池化的对象需要具备 `PoolObj`。
- 当前部分脚本会在运行时自动补 `PoolObj`，但更推荐后续在预制体上提前挂好。
- 切换场景或返回主菜单时需要清理对象池，避免旧场景对象残留。

## 11. 存档系统

存档由 `GameSaveStore` 管理，文件保存为 json。

保存位置：

Application.persistentDataPath/save.json

保存时机：

- 每一关开始时保存快照。
- 玩家从游戏中点击退出并确认时，保留这一关开局快照。

存档内容：

- 角色 id
- 当前关卡
- 玩家战斗属性
- 玩家等级、经验、金币
- 已拥有遗物 id 列表

读档逻辑：

- 点击继续游戏时读取 json。
- 进入游戏场景后按存档关卡重新开始。
- 玩家属性和经验金币恢复到该关开始时状态，避免通过退出重进刷经验。
- 遗物通过 `relicIds` 重新加载配置并应用。

## 12. UI 系统

业务 UI 面板主要包括：

- `BeginPanel`：开始、继续、退出。
- `ChooseCharacterPanel`：角色选择。
- `GamePanel`：生命、经验、金币、等级、倒计时、关卡、Boss 血条。
- `ResultPanel`：胜利、失败、成功存活。
- `ChooseBuffPanel`：升级强化三选一。
- `ShopPanel`：遗物商店。
- `PlayerInfoPanel`：玩家当前属性。
- `ExitPanel`：游戏内退出确认。
- `TipsPanel`：提示信息。

UI 数据绑定主要通过脚本查找子节点并保存引用。`GamePanel` 每帧刷新玩家生命、经验、金币、等级和 Boss 血量。

## 13. 音频系统

背景音乐：

- 使用 `MusicMgr.PlayBKMusic("game")` 播放。
- 开始场景和游戏场景都会尝试播放同一首音乐。
- `MusicMgr` 会避免重复播放同一 BGM。

音效：

- 战士攻击命中：`sword-stab-body-hit`
- 玩家受击：`hit_impact`
- 弓手射箭：`arrow`
- 怪物被射中：`Beshoot`


## 14. 后续扩展方向

1. 将 Boss 行为拆成可组合模块。
2. 将关卡时间、Boss 关、商店关、击杀奖励配置化。
3. 为 UI 增加统一绑定层，减少硬编码节点名。
4. 为存档增加版本号和迁移逻辑。
5. 为遗物系统增加更细的效果分类和事件钩子。
6. 为怪物行为增加行为策略，例如近战、远程、冲锋、召唤、范围技能。
