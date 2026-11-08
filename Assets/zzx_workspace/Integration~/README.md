# Museum_261016_3 靠墙展台接入


目标场景：`Assets/261016/Museum_261016_3.unity`。

已加入源场景 `Assets/zzx_workspace/Scenes/Museum_FirstDay_ZZX.unity` 的 FirstDayExhibit 展台，包含信件、工作证、地图的三个选项、重置按钮、完成面板及对应交互。

- 展台位置 `(5.7, 0.145, 0.5)`，朝向 Y=90°，位于右侧端墙附近。原位置与鱼竿展位接近，因此只调整新展台位置。
- 空间测量范围约为 X=5.40–6.16、Y=0.15–2.23、Z=-0.52–1.52，位于现有场馆范围内，避开现有鱼竿和水池。无需修改博物馆长度。
- 使用目标场景现有活动 XR Origin、Main Camera、左右控制器和根节点 XR Interaction Manager。两个抓取对象及四个按钮显式引用现有管理器。
- 使用工作区内的 FirstDayJourney、FirstDayMapButton、ReturnToOriginalWhenNotGrabbed；新增 FirstDayInspectionHint 替代原预制体对其他工作区提示脚本的依赖。XRI、TMP 等基础组件继续使用现有包。
- 新预制体 `Assets/zzx_workspace/FirstDayExhibit/Prefabs/FirstDayExhibit_261016.prefab` 保留原展台结构、文本和资源引用。原源场景和原预制体均未修改。
- `Assets/zzx_workspace/` 外，仅目标场景有修改。其所有原有对象保留逐字节原始内容，只新增展台实例及场景根引用。

## 操作

使用现有控制器 Grip 抓取信件或工作证，释放归位；射线指向地图选项后按 Trigger 选择。选中 Berth 02 显示结局；Restart 重置。

## 验证

Unity 2022.3.62f3 隔离物理副本编译及 Play 模式检查通过：唯一活动 Rig/主摄像头/Manager；左右控制器射线共用管理器；新增展台无缺失脚本；两份文件分别由左右手选择、释放及归位；错误答案、正确答案、完成锁定和重置。

隔离验证临时关闭硬件启动，并在运行时启用现有左右控制器分支以模拟无设备输入；这些验证设置未写回原项目。程序化 XR 选择/释放和任务状态检查不等同于真实设备按键测试，仍需佩戴头显确认操作手感和阅读距离。

详见 `validation.txt`、`PreservationReport.txt`、`survey.txt`。此目录以 `~` 结尾，Unity 不导入其中的检查记录和备份。`FirstDayMuseumIntegrationChecks.Smoke` 只允许在可丢弃的批处理副本中执行，结束后会退出 Unity。
