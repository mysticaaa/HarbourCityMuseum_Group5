# 当前接入版本（2026-10-15）

以远端 main 的 vensail_workspace/Scenes/VensailScene.unity 为展厅交互基准。
使用菜单 ZXH > Open vensail museum with cabin，打开 zxh_workspace/Scenes/Museum_Vensail_Cabin.unity。
此场景是队友场景的副本，仅船模型上的 ship_museum 组件替换为 VensailCabinEntry。
射线悬停船体后按原控制器的 Activate（扳机）登船，握持、移动、转向、头盔及其他展项沿用队友场景。
船体内继续使用独立 ShipInterior 场景，返回后还原玩家站位及展厅 UI 输入状态。
不在展厅创建旧版欢迎/故事/头盔面板，不接管公共相机、XR Origin、控制器或交互管理器。
新增与修改的脚本均位于 zxh_workspace；vensail_workspace 和 Assets/Scripts 原文件不修改。
构建设置需要包含 Museum_Vensail_Cabin 与 ShipInterior；菜单会追加注册而不会删除队友场景。
原始本地开发完整保存在 Git stash 与 Logs/LocalBackups；旧版构建包尚未重新生成。
以下为此前完整原型的历史实现说明，运行入口以上方接入版本为准。
# 可玩版本开发与验收说明

本轮开发位于本地 main，尚未提交或推送。入口仍为 Assets/Scenes/Museum.unity；没有改造船形博物馆外观。

## 如何运行

1. 用 Unity 2022.3.62f3 国际版打开项目，等待编译。
2. 打开 Museum 场景，进入 Play。展柜入口、欢迎说明、故事和头盔观察面板由 QuestExhibitSetup 创建。
3. 射线点击船表面仍使用原有局部放大，不经过新菜单。展柜旁单击 Board the fishing boat，先进入原船模的船尾工作甲板。沿右侧台阶打开驾驶室滑门，可连续行走进舱。
4. 船舱入口场景已经生成并加入 Build Settings：Assets/zxh_workspace/Scenes/ShipInterior.unity。它在运行时生成教学船舱和交互对象，编辑模式只显示启动根对象。这不是美术完成的写实场景。
5. 如船舱场景丢失，先退出 Play，然后执行 ZXH > Build playable cabin。菜单不会覆盖已有船舱场景。
6. 设备构建前执行 ZXH > Quest 3 > Configure Museum OpenXR。此电脑缺少 Android Build Support，尚未构建 APK；构建机需安装 Android SDK、NDK、OpenJDK。

Windows PCVR 开发包已构建成功：Builds/ZxhMuseum-PCVR/HarbourCityMuseum.exe。必须保留同目录的 Data、UnityPlayer.dll 及其他依赖，不能只发 exe。连接并开启 Quest Link 后，确认 PC 的 OpenXR runtime 已设置为 Meta，再启动程序。包体构建成功不代表设备验收通过。

## 本轮实现

- 保留现有 Museum 头盔抓取归位、船表面点击局部放大与闲置旋转。
- 增加独立入口、确认、取消、加载检查、淡入淡出、回程姿态保存、反复进出防重入。
- Museum 常驻，ShipInterior 加法加载和卸载；室内隐藏 Museum 展示根对象，保留唯一 XR Rig、交互管理器和 UI EventSystem，回程恢复原根对象。
- 默认关闭平滑移动／转向，保留现有瞬移、分段转向；船舱地板和两个站位提供瞬移交互。B/Y 返回或离座。
- 固定船舵可按住扳机远程拖动，轮缘和辐条有实际碰撞体；单手选择、轴心死区、追踪丢失冻结、最短角差、±135°限位。左右按钮与抓取修改同一份状态，右转定义为正舵角。
- 指南针和放大副本响应同一教学航向，提供 N/E/S/W 标识和数值。红色指针／刻度盘按负航向旋转，船舱保持固定；演示改航向在任务运行时被禁止。
- 动力档位、动力比例、速度、舵角与航向联动；停止时不原地转向；换向需要减小动力、回空档和低速。
- 三条可选航线、±10°目标容差、保持三秒完成、取消、重置、完成提示；船钟可作为完成反馈。
- 沙发确认后虚拟坐下，补偿头部水平偏移和观察高度；保留分段转向，提供离座面板及 B/Y，淡出离座后恢复原站位与移动组件。
- 世界空间 Canvas、XRI UI 输入、大按钮、背面隐藏防止镜像文字；避免多个活动 EventSystem。
- 头盔免持观察副本、左右转动按钮、五项知识说明、查看进度。知识按钮是说明入口，不冒充已核实的空间结构热点。
- 三张有来源的海事主题卡、访问进度、五分钟提醒、摘要、重新开始。原 Museum 时间线场景不被删除；新卡是附加教学内容。
- 抽屉把手、前板和托盘沿单轴限位移动；柜内扳手和仪表具备真实射线抓取及归位，不能从关闭的抽屉中取出。
- 航海日志三页及首末页边界、照片占位说明、船钟冷却与生成音效、收音机环境声／文字讲解／关闭。
- 柜体、墙体、窗、驾驶台等固定结构可点击用途说明；细小操作物悬停高亮；大型结构不整面变色。
- 柜内扳手与仪表已改为真实 XRI 远程抓取，完整打开抽屉后才可取出；松手平滑归位，工具持有／返回期间锁定抽屉，重置释放选择并恢复父物体、角度及缩放。
- 统一 ObservationService 支持照片和工具的渲染副本检查；切换目标关闭旧面板及副本，B/Y 优先关闭观察再返回区域。
- 增加通用发动机外壳／剖面切换、活塞／连杆／曲轴演示、播放／暂停／重置。它是新制作的教学模型，不是原船真实发动机或维修教程。
- 增加七段英语合成旁白及配套字幕：欢迎、船舱、船舵、头盔、港口工作、迁移和口述历史。由 Windows Microsoft Zira 生成，不是历史录音或真人录音。字幕内容读取同一 JSON；按片段显示，时间分配是近似值。
- 每个交互对象带唯一 InteractionRecord，记录 ID、交互类别、说明和来源性质。
- 重置释放已选对象并恢复航向、舵角、抽屉、日志和声音状态；离开船舱卸载生成物和音效资源。

## 代码职责

- Scripts/Playable/MuseumSession.cs：导览、展柜入口、进出、座位与进度。
- Scripts/Playable/CabinEnvironment.cs：教学船舱布局、控制面板与道具。
- Scripts/Playable/VoyageState.cs：独立于玩家视点的教学动力和航向模型。
- Scripts/Playable/WheelRayControl.cs：XRI 单轴船舵输入。
- Scripts/Playable/WorldPanel.cs：世界空间 UI、按钮和 EventSystem。
- Scripts/Playable/InspectionReplica.cs：仅复制渲染数据，不复制原件交互／刚体的观察副本。
- Scripts/Playable/ExhibitHighlight.cs：无材质实例泄漏的悬停颜色反馈。
- Scripts/Playable/ReturnableTool.cs：抽屉内工具的抓取、访问条件和自动归位。
- Scripts/Playable/ObservationService.cs：排他的渲染副本观察。
- Scripts/Playable/EngineCutaway.cs：新制作的通用机械剖面演示。
- Scripts/Playable/NarrationPlayer.cs 和 Resources/ZxhNarration：导览音频与字幕；Tools/GenerateZxhNarration.ps1 保存生成逻辑。
- Scripts/Interaction/Editor/PlayableCabinBuilder.cs：生成并注册船舱场景。
- Scripts/Interaction/Editor/PlayableFlowValidation.cs：完整流程模拟验证。

没有覆盖 zzx_workspace 的桌面原型。新的 VoyageState 是不含 OnGUI 的 Quest 教学状态，接口与行为参考团队导航原型；后续合并公共导航模块时应统一状态源，而不是同时运行两套模拟。

## 实际验证与限制

Unity 2022.3.62f3 已执行 Play Mode 批处理测试，包括：

- 十次船舱进入／回程，无活动 XR Rig 重复；UI EventSystem 在船舱仍然有效。
- 回程位置和方向不漂移，室内场景卸载，进度和重玩状态正常。
- 舵角正负限位，停船不转向，动力改变速度和航向，不允许高动力直接换向。
- 指南针 090／270°状态、任务跨越 359／0°边界、保持时间判定。
- 虚拟坐下头部高度、离座和重置。
- 原有头盔移动／释放／归位途中重新抓取，以及船表面命中、2倍放大、选中点居中、手动和超时恢复的回归测试。
- 使用 Unity 图形渲染生成船舱预览并检查，修复了背面镜像 UI 和座位界面朝向。
- 新增工具验证：关闭抽屉不能取物、打开后抓取、移动和转动后归位、返回期间不可关闭抽屉、复位姿态及观察副本排他性。
- 新增内容验证：交互 ID 不重复、七段音频可加载且长度有效、字幕加载匹配文本、机械外壳／播放／重置。
- Windows PCVR 开发包构建成功，约400MB；独立程序无头启动检查退出码0，确认会话、UI、船舱场景和旁白资源均已包含。日志为 Logs/ZXH-Player-Smoke.log。此检查未模拟头显或真实手柄。

自动测试没有模拟完整 Quest 硬件输入，不能替代手柄射线/UI点击、头显双目、实际追踪丢失、晕动舒适性、Android/IL2CPP构建及72Hz性能测量。保持“已通过／失败／未测”的区分。

本版已包含主流程、工具真实抓取、语音导览和教学机械剖面，仍不等于全部需求的最终美术／内容验收。尚未完成：真实历史船舱资产替换、授权照片／历史录音、精确绑定到已核实头盔部件的空间热点、特定船型真实发动机复原、更多语言和 Quest 真机性能验收。合成旁白和通用机械演示不能冒充这几项已完成。船形博物馆、船首门、日夜天气按需求仍为后期项目，未实现。

## 内容来源及许可

以下页面仅用于事实核实和内容概括，未下载或使用其图片、录音或视频。相关例子属于国际海事案例，不宣称这是当前模型或虚构港城的历史。

- London Museum，Helmet, diving helmet，DK86.21/2，生产日期1909–1935；其说明记录了港口潜水作业和通信装置。https://www.londonmuseum.org.uk/collections/v/object-260946/helmet-diving-helmet/
- Royal Museums Greenwich，History of the Windrush，1948年6月22日抵达 Tilbury 的迁移案例。https://www.rmg.co.uk/stories/ocean/history-empire-windrush
- Royal Museums Greenwich，Oral histories，海事工作、迁移及海上生活的口述历史资料。https://www.rmg.co.uk/collections/our-collections-oral-history

船舱、航海路线、日志、工具和声音由本项目教学示意生成，不是历史复原或真实操作培训。

## 队友设备验收顺序

1. 先验证 Museum：左右手点击船体放大，抓取头盔，松手归位和免持观察。
2. 独立登船按钮先结束放大，再到船尾甲板；工作甲板的控制面板提供返回博物馆。
3. 瞬移到驾驶台；验证轮缘握持，跨角度边界不跳变，左右按钮和回中一致。
4. 启动 Ahead 并增加动力；转舵看航向和指南针，停船确认不持续转向。
5. 选择路线并完成或取消任务；放大指南针与原件读数一致。
6. 沙发确认坐下，再通过近处离座按钮和 B/Y 返回；不要在现实中盲坐。
7. 检查抽屉、工具、日志、照片说明、船钟、收音机及静音。
8. 持物返回、坐着返回、连续十次往返、重玩，记录异常与设备帧率。

目前可按 main 的本地工作目录运行；未自动发布、构建或推送这些改动。


## 2026-10-13 原船模型一致性与连续行走

- 展柜旁单击 `Board the fishing boat`，淡出后到船尾工作甲板；船表面点击放大保持原样。
- 船体使用展柜模型的实际网格和材质，不再使用自制矩形船体或双桅方帆。截图中的装置是捕鱼吊臂、桅杆和索具，本版不添加不存在的方帆。
- 放大后目标长度为 28 个 Unity 单位，仅用于适配体验尺度，不代表实际船只的历史尺寸。移除展柜旋转父节点的姿态，保证每次进入船体方向一致。
- 原驾驶室玻璃范围经网格检查校准，内部位于船体前部。沿右侧有扶手的台阶走到滑门，点击门或入口按钮开门，再使用 WASD / Quest 左摇杆行走进入。可保留瞬移、分段转向及无障碍进舱快捷按钮。
- 左右原捕鱼吊臂各独立调节，范围 -10 至 +15 度、每秒 5 度；索具保持桅杆端锚点并跟随吊臂端，选择碰撞网格同步更新。属于受限演示动作，没有进行真实渔具力学仿真。
- 从原网格分离工作甲板舱盖和左侧绞盘，保留 UV 与原材质。舱盖可开合，绞盘每次点击转 30 度；鼠标与 Quest 射线均可操作。
- 固定船壳、桅杆及结构可点击观察；望远镜支持抓取归位和鼠标观察面板，没有光学缩放。
- 原网格只在新通道和室内空间做局部几何裁切；新增门、台阶、扶手及内部家具是体验补充，原模型资产未覆盖或修改。
- 行走使用 CharacterController 碰撞、重力及台阶处理，不通过直接移动坐标穿墙。离座后恢复行走；跌出船体时恢复到甲板起点。
- 海面为静态背景加装饰波纹；船体不会强制摇晃玩家。
- 自动验证检查原船网格、甲板落点、真实步行进舱、吊臂角度限制、原舱盖/绞盘动作、工具归位、导航和十次进出。设备帧率、真实握持手感与 Quest 3 硬件仍需实测。

所有新增开发保存在 zxh_workspace；尚未提交或推送。

本轮验证证据：
- Logs/ZXH-OriginalBoatEditorVerified.log：原船甲板入口、实际步行进舱、吊臂限位、原舱盖/绞盘动作和既有流程。
- Logs/ZXH-OriginalBoatBuild.log：Windows 开发包构建成功。
- Logs/ZXH-OriginalBoatPlayerComplete.log：独立程序原网格可读、登船、捕鱼装置、实际步行进舱及返回全部通过（无头显、无图形自动流程）。
- Logs/ZXH-Deck-Preview.png、ZXH-Cabin-Preview.png、ZXH-WalkableBoat-Preview.png：编辑器运行截图。
- 未构建 Android APK；未进行 Quest 3 硬件验收。
- 最终补充验证通过：船舵正向使轮盘顶部向访客右侧转动；实际鼠标 UI 射线在关门时被阻挡、开门后恢复命中；捕鱼竿网格不归入运动吊臂。快速登船时桌面预览不会把玩家重新移回展厅，连接头显后按实际 Floor / Device 追踪原点恢复相机高度。
- 指南针使用 Visual 子层校准，零航向时 N 指向船首，数据旋转轴保留零初始姿态；驾驶室前窗沿用原透明玻璃并能看见实际海面，移除蓝色占位窗。

## 恢复原场景控制方式
Quest 使用原 Museum 的左摇杆连续移动（速度 2）、右摇杆 45° 转向，握持键选择／抓取实体，食指扳机点击 UI。原移动组件不再被关闭，船上自定义行走仅用于无头显的桌面预览。旧 Windows 构建包尚不包含本次恢复，需要重新构建。

## 接入回归验证（2026-10-15）
Unity 编译通过。Logs/ZXH-Vensail-Final.log 中 ZXH VENSAIL CABIN PASS：三次登船/返回、玩家站位与 XR 组件启用状态恢复、船内交互管理器匹配、原船网格及舱盖绞盘生成检查通过。接入副本与 vensail 原场景仅船模入口组件 GUID 不同。Quest 真机输入手感、画面和帧率仍需实测。未提交或推送。

## 船内按钮交互更新（2026-10-15）
船内主要点击物和菜单均使用 BoatTriggerAction：沿用 vensail 的 XRI 悬停 + 控制器 Activate（扳机），不修改手柄绑定。抓取头盔/工具/船舵仍使用原抓取方式。菜单使用物理按钮碰撞体，鼠标与 Quest 共用回调，避免独立 UI Press 配置和重复触发。射线悬停会高亮按钮。
甲板索具按钮：Raise / Stop / Lower（点击启动平滑升降、限位停下）；Deck hatch：Raise hatch / Lower hatch（原中部舱盖抬起和放下）。保留原渔船索具外观，不新造方帆。驾驶台拆为 Helm（Left/Centre/Right）、Engine（Ahead/Stop/Astern），航线菜单移至侧壁。
所有本轮脚本位于 zxh_workspace；原 vensail 与公共脚本不改。

本轮验证日志：Logs/ZXH-BoatButtons-PlayLoop.log。ZXH BUTTON PASS：在 Play 的 Update 循环模拟 XRI 悬停与 Activate；仅悬停不触发，按下才升起；索具停止/下降限位、中部舱盖实体角度、按钮碰撞命中检查通过。ZXH VENSAIL CABIN PASS：三次往返及原 XR 状态恢复通过。Quest 实机手柄与画面仍需验收，未推送 GitHub。

## 确认登船与视角高度（2026-10-15 最新）
船模入口改为 Board the ship? 确认框：Enter ship / Cancel，防止打开弹窗的同次扳机直接确认。我们场景副本中的两处旧 Triggering 演示跳转已停用，避免与新入口竞争加载 Ship_vensail；队友原始场景和脚本保持不变。此前副本只替换一个组件的描述仅指早期版本。
BoatViewHeight 只在登船后调整 CameraFloorOffsetObject（头显与控制器一起），低视点参考约 1.65 米，保留真实头部追踪；返回后恢复原 CameraYOffset 和偏移，不改展厅配置。
Logs/ZXH-BoatConfirmation-Height.log：确认/取消/防连点、旧路由停用、船上抬高与展厅高度恢复、三次往返和升降按钮验证通过。Quest 高度舒适度仍需实测。

固定船壳、桅杆和墙体的结构观察/缩小副本功能已删除；保留实体碰撞、索具/舱盖操作和指南针/工具等专项观察。此条为最新行为，覆盖早期固定结构可点击观察的描述。

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
