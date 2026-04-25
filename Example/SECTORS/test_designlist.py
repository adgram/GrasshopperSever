from ghclient import GHClient

PORT = 6879

try:
    with GHClient(port=PORT) as client:
        commands = [
            'ap slider 0 100 _ "0.0 < 5.0 < 10.0" RadiusNum',   # 创建一个滑块，无需设置path，值以范围表示
            'ac Circle 300 100 CircleCurve',                    # 创建一个圆
            'cc RadiusNum _ CircleCurve Radius',                # 连接滑块和圆，滑块本身是param，无需第二个参数
            'ac Curve@Rectangle 300 250 Rect1',                 # 创建一个矩形，矩形和矩形param冲突，这里使用 分类@名称 创建
            'ap slider 300 400 _ " 0 < 10 < 100" SliderCount',  # 创建一个滑块
            'ac "Populate 2D" 500 250 Pop2D',                   # 创建一个二维随机点
            'cc Rect1 Rectangle Pop2D Region',                  # 连接矩形和随机点
            'cc SliderCount _ Pop2D Count',                     # 连接滑块和随机点
            'ac Distance 700 250 Dist1 ',
            'cc Pop2D population Dist1 "point A" ',
            'cc CircleCurve Circle Dist1 "point B"',
            'ac Bounds 700 400 Bounds1 ',
            'cc Dist1 Distance Bounds1 Numbers ',
            'ac "Remap Numbers" 900 400 Remap1 ',
            'cc Dist1 Distance Remap1 Value ',
            'cc Bounds1 Domain Remap1 source',
            'ap Panel 700 400 _ "0.2 to 0.5" Panel1 ',
            'cc Panel1 _ Remap1 target',
            'ac Circle 1100 250 CircleGen ',
            'cc Pop2D population CircleGen Plane ',
            'cc Remap1 Mapped CircleGen Radius'
        ]
        responses = client.send_command(
            name="DesignList",
            info="曲线干扰：根据点到曲线的距离控制圆半径",
            value= " ".join(commands)
        )
        print(f"响应：{responses}")
except Exception as e:
    print(f"错误：{e}")