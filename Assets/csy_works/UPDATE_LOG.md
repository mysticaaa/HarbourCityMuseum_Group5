# csy_works 更新记录

## 2026-10-08 Module 2 场景搭建

### Commit 4cd3edc — 创建个人工作区
- 建立 `csy_works/` 目录结构（Scenes/Models/Materials/Textures/Prefabs/Scripts/Lighting）
- 每个子目录放 `.gitkeep` 保留空文件夹
- 写 `README.md` 说明用途和规则

### Commit 9f3f178 — 脚本 + 材质 + 场景 + 编辑器工具
**脚本（4 个）：**
- `FollowViewUI.cs` — 跟随 XR 相机的 World Space Canvas，按 M 键弹出/隐藏，内置 Exit/Back 按钮
- `OperationHint.cs` — 走近自动弹出操作提示文字，支持 Proximity / Always 两种模式
- `CSYMaterialCreatorRuntime.cs` — 一键生成 9 个房间材质到 `csy_works/Materials/`
- `Editor/CSYMaterialCreatorEditor.cs` — Inspector 绿色大按钮触发材质生成
- `Editor/CSYFollowViewUICreator.cs` — 菜单 `CSY Tools > Create Follow View UI` 一键创建跟随 UI
- `Editor/CSYMaterialCreator.cs` — 菜单项（备用）

**材质（9 个）：**
| 材质名 | 用途 | 颜色 |
|--------|------|------|
| Mat_Floor_DarkWood | 地板 | 深棕 #3C2A20 |
| Mat_Wall_Cream | 墙壁 | 奶油 #F5F0E8 |
| Mat_Wainscoting_Teak | 墙裙 | 柚木 #8B6F47 |
| Mat_Ceiling_OffWhite | 天花板 | 米白 #F8F4F2 |
| Mat_DisplayPlatform | 展示台 | 深灰 #2C2C2A |
| Mat_Pedestal_Teak | 展品底座 | 柚木 #8B6F47 |
| Mat_TimelinePanel_Navy | 时间线展板 | 深蓝 #1A3A4A |
| Mat_BrassTrim | 黄铜装饰条 | 黄铜 #B8860B |
| Mat_Parchment | 羊皮纸文字背景 | 旧纸 #E8D5B0 |

**场景：**
- `Scenes/MuseumScene_CSY.unity` — 基于 SampleScene 的个人测试场景

### Commit 9a6b7bc — 修复 Canvas 文字镜像
- `FollowViewUI.cs` LateUpdate 和 ToggleVisible 中 `LookRotation` 后乘 `Euler(0,180,0)`
- `CSYFollowViewUICreator.cs` 初始旋转改为 Y=180°

### 材质应用状态
- 9 个材质已生成并应用到场景物体（Floor/Walls/Ceiling/TimelineWall 等）
- 需要在 Unity 中 Ctrl+S 保存场景后提交

### 搭档 hmn 头盔模型
- hmn_xr 分支提交 `0121bc9` 上传了潜水头盔 FBX 模型
- 模型路径：`Assets/hmn_workspace/Models/DivingHelmet 1.fbx`
- 已取到本地用于测试（untracked，不提交到 csy_xr）
- 纹理贴图在 `Assets/hmn_workspace/Models/Textures/` 和 `Assets/Texture/`

### 待 push
- 3 个 commit（4cd3edc, 9f3f178, 9a6b7bc）在本地 csy_xr 分支
- 需在自己的终端执行 `git push origin csy_xr`
