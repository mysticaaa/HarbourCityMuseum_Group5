# 桌面港口原型

## 打开与操作

打开 `Assets/261122/Museum_261122_1.unity`，等待 Unity 导入脚本后进入 Play。
原场景中的 FirstDayJourney 会在这个场景内添加 HarbourTabletopPrototype。
编辑模式仍显示原展台；进入 Play 才生成微缩港口并隐藏原地图、文档和答题按钮。
退出 Play 恢复原编辑场景。无需修改或保存目标场景文件。

1. 点击 START / CHECK IN，点击 WORK PASS。
2. 按高亮提示探索 FERRY、CARGO、REPAIR。每区都有船只移动演示，动画结束才记录已访问；可自行改变探索顺序，下一步会引导至尚未完成的区域。
3. 三个区域全部演示结束后，点击 START / REPORT 完成报到；未探索完不能跳过此步骤。
4. 点击 CRATE，再点击船上 LOAD HERE，观看起吊、横移、下降和放货。
5. 点击 LETTER HOME 完成故事。随时 RESTART；装货中可 SKIP ANIMATION。

家书是固定的同一封信，操作前后内容一致；完成装货后再读信，仅追加独立的完成提示。重置会清除三个区域的完成记录、高亮和所有船只位置。

支持现有 XR 控制器的 selectEntered，以及项目原有 hover + activateAction 输入。
鼠标左键使用现有 MainCamera 射线。需要视角面向展台，不会自动传送玩家。

## 范围与集成

所有本次源代码修改位于 Assets/zzx_workspace。目标场景、其他工作区和场馆长度未修改。
生成模型在展台本地 x=-0.93..0.93、z=-0.34..0.40 附近，主体位于现有台面范围。
不创建 XR Rig、Camera、控制器、XRInteractionManager，也不修改输入绑定。
新交互从原展台 XRBaseInteractable 获取已有 interactionManager；缺失时停止并报错。
原型仅在指定场景运行，其他场景继续使用原 FirstDayJourney。

## 实现与限制

使用基础几何体和运行时材质，无外部素材。吊钩使用固定路径协程，绳索使用 LineRenderer。
文字为英文，沿用项目现有 TMP 字体。历史人物和家书为情境演绎。
本版通过点击面板查看工作证和家书，没有新增抓取、配音、真实印章或信件折叠动画。
尚未制作精细船模、建筑和水面效果。

## 验证

已使用 Unity 2022.3.62f3 自带 Roslyn 和项目已有 Assembly-CSharp 编译参数编译全部运行时脚本。
零编译错误；两条既有警告位于其他工作区 OperationHint 和 TimelineStation。
git diff --check 通过。
未实际运行 Unity Play/头显测试；点击遮挡、显示大小、坐姿可达性和实际场景效果仍需实机确认。
本次开始前已有 .gitignore、ProjectSettings/ProjectSettings.asset 修改，保持不动。
