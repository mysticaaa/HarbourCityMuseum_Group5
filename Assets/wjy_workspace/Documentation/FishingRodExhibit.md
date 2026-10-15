# 鱼竿展品

打开本目录上级的 `Museum.unity`。

- 新展位：`WJY_FishingRodStation`，位于头盔另一侧，坐标约 `(3.2, 0.143, -0.75)`。
- 鱼竿约 2.20 米高，竖直放置；底端离地约 1.00 米，比展台顶面高约 0.40 米。
- 白色展台沿用现有头盔展台的网格和尺寸，宽深约 0.714 米、高约 0.453 米，独立于旋转父节点。
- 运行时由 `ExhibitAutoRotation` 的同一个 `FixedUpdate` 函数旋转，默认 15°/秒；头盔和渔船继续使用原有组件。
- 新增展示灯只服务鱼竿展位。

## 调节房间长度

在 Hierarchy 选中 `wip_-_exhibition_room`，修改 `Museum Room Length` 组件中的 `Length Metres`。
当前长度约 20.208 米，由原先约 16.166 米增加 25%。该组件仅调整房间壳体 `PIECE` 的水平长度，不缩放展台、头盔、渔船或鱼竿；房间宽度和高度保持原值。缩短后需要自行确认展品仍位于墙内。

## 模型与授权

采用 Justin Randall 的公开模型 Fishing rod，来源：https://poly.pizza/m/9gXWYDqB6vt 。
保留原始 GLB，并转换为 OBJ，调整尺寸、底部轴心和竹木／黄铜／软木配色，制作复古风格展品。这是艺术改制，不是经过历史考证的古董扫描模型。
完整署名与 CC BY 3.0 授权链接见 `Models/FishingRod/ATTRIBUTION.md`；分发项目时保留该文件。

## 验证

在 Unity 2022.3.62f3 的独立项目副本中完成导入、场景保存和渲染，避免原项目自动初始化脚本改写其他资源。
对三个展品的共享旋转组件各执行 100 次 `FixedUpdate`，均旋转 30°，暂停检查通过。详见 `rotation-report.txt`。
这些是旋转函数和场景渲染检查；没有完成 Quest 3 设备实测。

可选的 `WJY > Museum > Rebuild fishing rod exhibit` 菜单可重新生成鱼竿展位，会替换当前鱼竿展位并保存此场景。
