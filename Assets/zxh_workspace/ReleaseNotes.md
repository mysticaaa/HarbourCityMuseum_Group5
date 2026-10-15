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
