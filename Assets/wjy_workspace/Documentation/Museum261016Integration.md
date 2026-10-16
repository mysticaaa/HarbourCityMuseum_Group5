# Museum_261016 鱼竿展位接入

目标场景：`Assets/261016/Museum_261016_2.unity`。源场景 `Assets/wjy_workspace/Museum.unity` 保持原样。

## 场景接入

- 复用 `Assets/wjy_workspace/Prefabs/TraditionalFishingRodExhibit.prefab`，包括竹木鱼竿、卷线轮、鱼线、鱼钩、白色展台、水池和展示灯，位置 `(3.2, 0.1427, -0.75)`。
- 使用目标场景已有 `XR Interaction Hands Setup/XR Origin (XR Rig)`、其 Main Camera 和左右控制器；普通 XR Origin 仍保持禁用。
- 使用场景根节点的 XR Interaction Manager。将 Hands Rig 内的射线、近距离交互器和 Interaction Group 指向它，禁用 Hands Setup 内重复的 Manager 节点。仅保存为目标场景的实例覆盖，没有改共享预制体。
- 房屋外壳 `PIECE` 的长度从约 16.166 米扩大到 20.208 米，只有外壳局部 Y 比例从 200 调整到 250；房间根节点挂载本目录现有 `MuseumRoomLength`。头盔、船、展台和视频屏幕尺寸与位置保留。
- 鱼竿交互使用本目录的 `WjyFishingRodGrab.cs` 和 `WjyFishingRodInteraction.cs`。通过控制器祖先节点识别左右手；只在持竿期间临时启用射线 Force Grab，释放后恢复。共享 Input Actions 和其他展品脚本均未改动。

## 操作

射线指向鱼竿，按住 Grip 拿起并停止展示旋转；保持 Grip 移动、转动手柄查看。按住 Trigger 挥竿，再松开 Trigger 抛钩入池。松开 Grip 后鱼竿、鱼钩自动归位，恢复展示旋转。

## 验证

使用 Unity 2022.3.62f3 的物理隔离副本完成编译、预览和 Play 模式模拟。隔离副本关闭硬件 XR 自动启动并禁用无关编辑器初始化器，不将这些配置写回原项目。

`Museum261016Validation.txt` 记录左右手射线命中、Grip 手动选择、手柄旋转跟随、Trigger 挥竿抛钩、鱼线连接、入水、释放归位、选择绑定不变及 Force Grab 恢复等检查通过。验证确认只有一个活动 XR Rig 和一个有效交互管理器。仍需佩戴 Quest 3 检查实际握持姿态和操作手感。

目标场景原有视频控件存在父子交互器重复注册同一碰撞体的警告，另有缺失脚本引用提示；这些不属于本次鱼竿接入范围，未调整相应逻辑。

新增编辑器工具 `WjyMuseum261016Integration` 与 `WjyMuseum261016Validation` 用于在独立项目副本重建和检查，不自动执行。不要在有未保存场景修改的项目中手动执行重建。
