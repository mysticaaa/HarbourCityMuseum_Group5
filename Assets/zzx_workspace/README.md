# 靠墙展台：初到港城

场景：`Assets/zzx_workspace/Scenes/Museum_FirstDay_ZZX.unity`
Prefab：`Assets/zzx_workspace/FirstDayExhibit/Prefabs/FirstDayExhibit.prefab`

## 物品清单与交互方式

| 物品 | 交互组件 | 怎么交互 |
| --- | --- | --- |
| 家书（Letter） | XRGrabInteractable + BoxCollider + Rigidbody(kinematic) + ReturnToOriginalWhenNotGrabbed + ShowObjectNameOnGrab | 手柄射线指向后 Grab 抓起查看；抓取时激活对应的 Inspection hint 提示文字；松手后瞬移回原位、锁定物理、隐藏提示 |
| 工作证（Work pass） | 同上 | 同上；文档内容为“REPORT TO BERTH 02” |
| 地图 01 FERRY | XRSimpleInteractable + BoxCollider + FirstDayMapButton(berth=1) | 手柄射线悬停按钮，按 Trigger 触发；反馈“TRY AGAIN / ferry landing is where he arrived”，可重新选择 |
| 地图 02 CARGO | XRSimpleInteractable + BoxCollider + FirstDayMapButton(berth=2) | 手柄射线悬停按钮，按 Trigger 触发；答对，显示结局面板 |
| 地图 03 REPAIR | XRSimpleInteractable + BoxCollider + FirstDayMapButton(berth=3) | 手柄射线悬停按钮，按 Trigger 触发；反馈“TRY AGAIN / repair yard services boats”，可重新选择 |
| RESTART | XRSimpleInteractable + BoxCollider + FirstDayMapButton(berth=0) | 手柄射线悬停按钮，按 Trigger 触发；重置答题进度、隐藏结局面板 |
