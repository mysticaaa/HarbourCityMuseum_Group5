# zxh 船体探索交付说明 — 2026-10-15

本次代码位于 Assets/zxh_workspace。沿用 vensail_workspace 的展厅交互，在独立副本中接入船体探索，不替换默认 Museum 场景。

## 队友如何运行

1. 拉取 main，用 Unity 2022.3.62f3 打开，等待编译完成。
2. 打开 Assets/zxh_workspace/Scenes/Museum_Vensail_Cabin.unity，或菜单 ZXH > Open vensail museum with cabin。
3. 连接 Quest Link 后 Play，射线指向船模按扳机，选择 Enter ship；Cancel 留在展厅。
4. 登船后从甲板步行或使用快捷按钮进入船舱；菜单、实体点击沿用悬停加 Activate，抓取保留原握持绑定。
5. Return to Museum 返回原站位，并恢复展厅视角高度与输入状态。

## 本次内容

- 原船模网格生成的可行走甲板、船舱、大海、索具及操作物。
- 索具 Raise / Stop / Lower，舱盖 Raise hatch / Lower hatch。
- 船舵、指南针放大、座位、工具归位、抽屉、航线及教学发动机。
- 确认登船、防止同次扳机误确认，副本中停用旧 Ship_vensail 演示跳转。
- 船上临时提高观察高度，返回还原；固定船壳、桅杆、墙体不再弹出说明或小船副本。

## 对 main 的影响范围

- 新增的代码和资源均在 zxh_workspace；队友原脚本和场景不改。
- ProjectSettings/EditorBuildSettings.asset 仅追加 Museum_Vensail_Cabin 和 ShipInterior，已有顺序和默认启动场景不变。
- 不提交本机 XR 配置、播放器配置、Library、日志、备份和 Builds。
- 新场景资源会增加项目导入和构建内容；不承诺完全无兼容风险。
- 旧 Windows 构建包未重新生成。APK 和 Quest 真机性能/交互舒适度仍需设备验收。

主要自动验证：VensailCabinValidation.Run。旧原型工具/说明保留供参考；当前入口与交付范围以本文件为准。

发布前已同步队友最新 main，并通过 Unity 编译、确认/取消/防误触、索具及舱盖按钮、固定结构不再弹窗、视角恢复与三次往返验证。日志保存在本机 Logs/ZXH-MainRelease-Verified.log（不上传）。Quest 实机性能和舒适度仍需测试。

## 本地实体交互版（2026-10-15，本轮未提交）
- 索具控制台在船尾右侧，避开中央舱盖旁的主通道。射线指向黄色握把，按住扳机，向船头方向移动手柄升起，回中停止，向船尾方向移动下降；松开扳机自动回中。控制台另有实体 STOP 按钮。无头显可鼠标按住握把拖动。
- 中部舱盖旁有独立实体操作台；HATCH 按一次抬起、再按一次放下。旧索具和舱盖悬浮菜单已移除。
- 门内外各一个实体按钮，按一次打开，再按一次关闭；站在门洞时保留防夹关闭限制，门板本身不再触发开关。
- 射线点击沙发直接虚拟坐下，扶手旁 LEAVE SEAT 实体按钮离座并恢复原站位，B/Y 仍可离座。只改变虚拟姿态。
- 驾驶室前窗增加可翻开的窗扇和固定窗扣，点击窗扇/窗扣打开，再点窗扣关闭，看到实际场景海面。
- 实体按钮仍用 vensail 的 XRI 悬停 + Activate（扳机）逻辑，不改原移动和握持绑定。标签按实际尺寸排版、正常深度遮挡，并支持 XR 双眼渲染；着色器放在 Resources 确保构建包含。
- 验证：Logs/ZXH-PhysicalControls-Delivery.log 的 PHYSICAL CONTROLS、PHYSICAL SEAT、VENSAIL CABIN PASS；控制杆实际模拟输入、居中/松手停止、升降限位、门/舱盖切换、窗扇、座位及三次往返通过。Logs/ZXH-PhysicalLabel-Validation.log 验证标签渲染无着色器编译错误。Quest 真机手感、双眼和性能仍需设备验收。
- 本轮代码、着色器和文档都在 zxh_workspace，未提交或推送。

## 右侧玻璃舷窗与推杆方向修正（2026-10-15，本地未提交）
- 移除墙面 Port story picture 占位图及 Port interpretation 观察入口。
- 驾驶室朝船首方向的右侧新增透明玻璃舷窗，实体 WINDOW 按钮或玻璃射线点击可切换开关；保留原前窗。
- 同步切开程序化墙体和运行时复制的外层船模，窗外为真实海景，原始船模资源不改动。
- 推杆拖动改为固定在初始射线朝向的平面，并按实际杆轴投影计算位移，避免水平面交点造成方向反转；保持前推升起、后拉下降、回中/松手停止。
- Unity 2022.3.62f3 回归通过：XRI 合成输入、横向移动不误触发、前后推杆、限位、按钮、两扇窗、座位及三次往返。预览图位于 Logs/ZXH-Physical-RightWindowClosed.png 和 RightWindowOpen.png。
- 日志：Logs/ZXH-RightWindow-Validation.log。未完成 Quest 真机手感验证，未提交或推送。

### 推杆可视方向二次修复（2026-10-15）
定位到 RiggingLever.SetDeflection 对 X 轴角度错误取负，使黄色杆头的实际位移与 +Z 输入相反；现已改为正角度。保留前推升起、后拉下降及松手停止。回归增加杆头实际世界位移同向断言，避免只验证控制状态遗漏视觉反向。本轮未 push。

## zxh 实体交互更新发布（2026-10-15）
本次提交包含上文实体交互版、右侧玻璃舷窗和推杆可视方向修复；“本地未提交”等措辞是此前开发阶段记录，本节为本次交付说明。

- 修改范围：仅 Assets/zxh_workspace，保留 vensail 的移动、射线 Activate 和握持配置，不上传本机 XR 设置或队友预制体修改。
- 索具：悬浮菜单改为实体三档推杆与 STOP 按钮，前推升起、回中/松手停止、后拉下降，并修正黄色杆头反向倾斜。
- 舱盖：实体操作台按钮，单击抬起、再次单击关闭。
- 驾驶室门：门内外实体按钮切换开关，保留门洞防夹限制。
- 沙发：点击坐下，LEAVE SEAT 实体按钮离座恢复站位，保留 B/Y 离座。
- 舷窗：前窗可翻开；新增驾驶室右侧透明玻璃窗及实体开关，开关窗均可看到实际海景，移除占位解说图和观察菜单。
- 实体标签：限制尺寸、正确深度遮挡，包含 XR 双眼着色器支持及构建资源。
- 场景：根据最新 vensail 场景重新生成 Museum_Vensail_Cabin 副本，接入已有船舱；队友原场景不修改。
- 验证入口：VensailCabinValidation.Run；发布日志 Logs/ZXH-PhysicalRelease-Verified.log（本地，不上传）。Quest 实际手感、双眼显示及性能仍需真机验收。
- 队友拉取后打开 Assets/zxh_workspace/Scenes/Museum_Vensail_Cabin.unity 测试本船舱入口。
