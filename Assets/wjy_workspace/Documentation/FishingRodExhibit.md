# 老式竹木鱼竿：Grip 抓取与 Trigger 抛竿

场景：`Assets/wjy_workspace/Museum.unity`。
展位：`WJY_FishingRodStation`，位置约 `(3.2, 0.143, -0.75)`。

## 模型

鱼竿已重新制作为独立的约 2.1 米竹木钓竿：竹节竿身、缠线、软木握柄、黄铜卷线轮和摇柄、导线环、鱼线、带眼及倒刺的 J 形鱼钩。几何由本目录的编辑器构建脚本生成，没有继续使用原 Poly Pizza 模型。

- `Models/TraditionalFishingRod/TraditionalBambooRod.asset`：新竿身及卷线轮网格。
- `Models/TraditionalFishingRod/FishingHook.asset`：新鱼钩网格。
- `Prefabs/TraditionalFishingRodExhibit.prefab`：展台、鱼竿、水池和展示灯。
- 鱼线由 `LineRenderer` 沿卷线轮、导线环、竿尖和鱼钩实时绘制。

旧 OBJ、MTL、GLB、旧模型材质、旧鱼竿预制体及其署名文件已移除。

## 操作

1. 使用现有左手或右手射线指向鱼竿，按住 **Grip** 拿起。竿柄贴合手柄，自动旋转停止。
2. 保持 Grip，移动或转动手柄，查看竿身、鱼线、鱼钩和卷线轮。
3. 保持 Grip，按住 **Trigger**，挥动鱼竿后松开 Trigger，鱼钩沿抛物线飞向小水池，鱼线持续连接竿尖与鱼钩。
4. 没有挥动时松开 Trigger 不抛竿。刚抓取时已按住的 Trigger 不会直接触发抛竿，需先松开再按。
5. 鱼钩入水后可再次按住 Trigger、挥动并松开重新抛竿。
6. 松开 Grip 自动收回鱼钩并归位展台，恢复展示旋转。手柄断连或应用失焦也会释放鱼竿。

这是馆内定点练习：挥动方向改变池内落点，挥动速度影响飞行速度，落点辅助限制在水池范围内。本版本没有增加钓鱼、咬钩或摇轮收线玩法。

## 共用与独立范围

共用当前场景已有的 `XR Origin (XR Rig)`、Main Camera、左右 XRRayInteractor 和有效 XRInteractionManager；没有新增 Rig 或摄像头。

- `Scripts/FishingRod/WjyFishingRodGrab.cs`：鱼竿专用的抓取对象，阻止 Trigger 单独选中鱼竿。
- `Scripts/FishingRod/WjyFishingRodInteraction.cs`：读取左右 XR 手柄的 Grip/Trigger；通过原有射线的手动选择接口抓取；实现展示旋转、抛钩、鱼线和归位。
- 共享的 `QuestExhibitSetup.cs` 会把控制器选择键设为 Trigger；本实现没有修改它或共享 Input Actions。鱼竿通过独立 Grip 桥接兼容此设置。
- 头盔和船的模型、交互脚本、按键及共享预制体保持原样。鱼竿不再依赖它们的 `ExhibitAutoRotation`、`HelmetHandInspection` 或 `ShipDetailInspection`。

房间长度组件保持现有设置；本次没有修改房间尺寸。

## 验证与重建

使用 Unity 2022.3.62f3 在本目录内的物理隔离副本进行编译、场景渲染及 Play 模式的左右控制器模拟测试，避免原项目自动初始化过程改写其他目录。
结果见 `FishingRodValidation.txt`。模拟测试不能替代实际 Quest 3 的握持姿态、抛竿手感及震动检查。

`WJY > Museum > Rebuild traditional fishing rod` 可以重建此展位，会替换鱼竿展位并保存目标场景。预制体的 Shared Rig 是场景引用，手动将预制体拖入其他场景后，需给该字段指定那里的既有主 XR Origin。
