# COMP5424 Phase 2 — Git 工作流与团队分工方案

## 1. 当前仓库状态

| 项目         | 状态                                            |
| ---------- | --------------------------------------------- |
| Unity 项目   | 已初始化（SteamVR + XR 配置完成）                       |
| .gitignore | 标准 Unity 模板，正确                                |
| 个人分支       | 5 个已建（wjy_xr, csy_xr, zxh_xr, wyh_xr, zzx_xr） |
| 缺失分支       | hmn_xr（Huang Mina）                            |
| 缺失分支       | dev（集成分支）                                     |
| Assets 内容  | 仅默认 SteamVR/XR 文件夹，无自定义代码/模型                  |
| README     | 空的                                            |

## 2. 建议的 Git 分支结构

```
main          ← 只放稳定可提交的版本（每个 Phase 截止时更新）
 └── dev       ← 日常集成分支，所有人的功能分支合并到这里
      ├── wjy_xr   (Wang Jingyi)
      ├── wyh_xr   (Wan Yuhang)
      ├── csy_xr   (Chen Siyu)
      ├── zxh_xr   (Zhou Xinhao)
      ├── hmn_xr   (Huang Mina — 需要创建)
      └── zzx_xr   (Zhang Zhixing)
```

### 工作流规则

1. **永远不要直接向 main 提交**。main 只在 Phase 截止时从 dev 合并。
2. 每个人在自己的分支上开发，完成后向 dev 发 Pull Request。
3. PR 需要至少一个人 review 后才能合并（可以轮值）。
4. dev 分支保持可编译状态。如果某次合并导致编译错误，必须立即修复。
5. commit message 格式：`[模块名] 简短描述`，例如 `[RaySelect] 实现射线选择基础逻辑`。

### 创建 dev 分支的命令

```bash
# 在本地 main 分支基础上创建 dev
git checkout main
git pull origin main
git checkout -b dev
git push origin dev

# 创建缺失的 hmn 分支
git checkout -b hmn_xr
git push origin hmn_xr
```

## 3. Assets 文件夹结构

在 Unity 的 Assets 目录下创建以下结构（由仓库管理员在 dev 分支上创建后合并）：

```
Assets/
├── Scenes/
│   ├── MainScene.unity          ← 主场景（三个站点都在这里）
│   └── InitScene.unity         ← 初始化/加载场景（可选）
├── Scripts/
│   ├── Interaction/            ← 射线选择、交互逻辑
│   ├── Station_Ship/           ← 船模型站点脚本
│   ├── Station_Helmet/          ← 潜水头盔站点脚本
│   ├── Station_Timeline/       ← 时间线墙脚本
│   ├── Audio/                  ← 音频管理
│   └── Utils/                  ← 工具类
├── Models/                     ← Maya 导入的 3D 模型
│   ├── Ship/
│   ├── Helmet/
│   └── Environment/
├── Materials/                  ← 材质
├── Textures/                   ← 贴图
├── Audio/                      ← 音频文件
│   ├── Voiceover/
│   └── Ambient/
├── Prefabs/                    ← 预制体
│   ├── Stations/
│   ├── UI/
│   └── Player/
├── Timeline/                   ← 时间线资源
├── SteamVR/                    ← （已存在，不要动）
├── SteamVR_Input/              ← （已存在，不要动）
├── SteamVR_Resources/         ← （已存在，不要动）
├── StreamingAssets/            ← （已存在）
└── XR/                         ← （已存在，不要动）
```

## 4. 第一周分工方向（待团队开会确认）

以下只是可能的分工**方向**，不是最终分配。团队开会讨论后再确认每人的具体任务。代码框架已经搭好，所有模块的脚本都已存在，分工主要是决定谁负责改进/完善哪个模块。

### 可能的任务模块

| 代号 | 任务方向 | 涉及脚本/文件 |
|------|----------|--------------|
| P1 | 射线选择交互完善与测试 | RaycastSelector.cs, InteractableObject.cs, StationTrigger.cs |
| P2 | 船模型 Maya 建模 + ShipStation 配置 | ShipStation.cs, Assets/Models/Ship/ |
| P3 | 潜水头盔 Maya 建模 + HelmetStation 配置 | HelmetStation.cs, Assets/Models/Helmet/ |
| P4 | 时间线墙 UI 布局 + TimelineStation 配置 | TimelineStation.cs, Assets/Textures/ |
| P5 | 音频素材 + AudioManager 配置 + 技术文档 | AudioManager.cs, Assets/Audio/ |
| P6 | 场景搭建 + 灰盒集成 + 整体测试 | MuseumSceneBuilder.cs, UIManager.cs, ExperienceFlow.cs |

**重要**：
- 以上 P1–P6 仅为任务模块代号，不对应任何特定成员。
- 实际分工以团队讨论结果为准，一人可负责多个模块，也可多人协作同一模块。
- 团队确认分工后，请将代号替换为实际负责人名。

## 5. 每个人的立即可执行步骤

### Step 1：克隆仓库并切换到自己的分支

```bash
git clone https://github.com/mysticaaa/HarbourCityMuseum_Group5.git
cd HarbourCityMuseum_Group5

# 切换到你的分支（替换成你的分支名）
git checkout <你的分支名>

# 确保分支是最新的
git pull origin <你的分支名>
```

### Step 2：在 Unity 中打开项目

- 用 Unity Hub 打开项目
- 等待 Unity 编译完成（第一次可能需要几分钟）
- 确认 SteamVR 正常加载

### Step 3：创建 dev 分支（由仓库管理员执行一次）

```bash
git checkout main
git pull origin main
git checkout -b dev
git push origin dev
```

### Step 4：创建文件夹结构（由仓库管理员在 dev 上执行一次）

- *在 Unity 的 Project 窗口中按上面*第 3 节的结构创建文件夹
- 提交并推送到 dev：

```bash
git add .
git commit -m "[Structure] 创建项目文件夹结构"
git push origin dev
```

### Step 5：所有人同步 dev 到自己的分支

```bash
# 每个人在自己的分支上执行
git checkout <你的分支名>
git merge dev
# 解决可能的冲突后
git push origin <你的分支名>
```

### Step 6：开始各自的开发任务

- 按第 4 节的分工开始工作
- 每完成一个小功能就提交一次（不要攒一大堆才提交）
- 提交后推送到自己的分支，然后向 dev 发 PR

## 6. PR 合并流程

```bash
# 在 GitHub 上操作
# 1. 你的分支 → dev
# 2. 写清楚 PR 标题和描述（做了什么、为什么）
# 3. 至少一个队友 review 后合并
# 4. 合并后所有人需要同步：
git checkout <你的分支名>
git merge dev
git push origin <你的分支名>
```

## 7. 第一周结束时的验收标准

- [ ] dev 分支存在且所有人已同步
- [ ] Assets 文件夹结构已创建
- [ ] MainScene 中有灰盒布局（三个站点位置已确定）
- [ ] 射线选择能命中占位物体并触发 Debug.Log
- [ ] 船模型灰盒已导入 Unity（至少外壳 + 甲板）
- [ ] 头盔模型灰盒已导入 Unity
- [ ] 时间线墙灰盒 UI 已搭建
- [ ] 每人至少有 3-5 个有意义的 commit
- [ ] 每人已在 README 或活动记录中记录本周工作

## 8. 常见问题

**Q: Unity 场景文件冲突怎么办？**  
A: Unity 场景文件（.unity）是 YAML 格式，容易冲突。尽量避免两个人同时修改同一个场景。建议：

- 一个人负责主场景的修改（团队指定一名场景负责人）
- 其他人用 Prefab 的方式开发各自的功能，最后由场景负责人集成
- 如果必须同时编辑，用 Unity 的 Smart Merge 工具

**Q: Maya 文件要不要放进 Git？**  
A: .ma 和 .mb 文件是二进制格式，不适合 Git 管理。建议：

- 只把导出的 FBX 文件放进仓库的 Assets/Models/ 目录
- Maya 源文件用网盘或其他方式共享
- 或者在 .gitignore 中忽略 .ma/.mb 文件

**Q: SteamVR 的输入配置怎么共享？**  
A: SteamVR_Input 文件夹已经包含输入配置。修改输入配置后要提交这些文件。建议团队指定一个人负责修改输入配置，避免多人同时改动产生冲突。

**Q: 提交频率怎么把握？**  
A: 评分标准明确说不看 commit 数量。建议：

- 完成一个独立功能或修复后提交一次
- commit message 写清楚做了什么
- 不要一次性提交大量文件改动
- 不要提交编译不通过的代码到 dev
