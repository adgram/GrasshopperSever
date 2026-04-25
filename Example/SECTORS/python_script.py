
from ghclient import GHClient


Script = """
# GH_COMPONENT_IO_START
# INPUT_PARAMS: [{"typeHintName": "float", "scriptParamAccess": 1, "variableName": "xs"},{"typeHintName": "float", "scriptParamAccess": 1, "variableName": "ys"}]
# OUTPUT_PARAMS: [{"typeHintName": "ghdoc Object", "variableName": "surface"}]
# GH_COMPONENT_IO_END

import rhinoscriptsyntax as rs
import math

# ======== 自定义数学函数 ========
def f(x, y):
    # 示例：正弦余弦组合曲面（可自行修改）
    return math.sin(x) * math.cos(y)

# ======== 主逻辑 ========
if xs is None or ys is None:
    surface = None
    print("输入无效：xs 或 ys 为空")
else:
    # 确保 xs 和 ys 被转换为列表（兼容 Grasshopper 数据树）
    if hasattr(xs, 'ToList'):
        xs = xs.ToList()
    if hasattr(ys, 'ToList'):
        ys = ys.ToList()
    
    # 生成点阵（按网格顺序：固定 y，遍历 x）
    points = []
    for y in ys:
        for x in xs:
            z = f(x, y)
            points.append((x, y, z))
    
    # 通过点阵创建 Nurbs 曲面
    points_2d = []
    for y in ys:
        for x in xs:
            z = f(x, y)
            points_2d.append((x, y, z))
    srf = rs.AddSrfPtGrid((len(xs), len(ys)), points_2d)
    surface = rs.coercegeometry(srf)
"""


if __name__ == '__main__':
    with GHClient(port=5695) as client:
        responses1 = client.send_command(
            "ScriptEditor",
            "测试GHPython3代码执行",
            {"OUTPUT": Script}
        )
        script_guid = GHClient.extract_value(responses1, "OUTPUTDATA")

        client.send_command(
            name="DesignList",
            info="创建组件",
            value='ap slider 100 50 _ 0.1 num1 ap slider 100 150 _ "0<100<100" num2 ac series 400 50 series1 ac series 400 150 series2 ac panel 800 100 panel1'
        )
        responses2 = client.send_command(
            name="DesignList",
            info="连接组件",
            value=f'cc num1 _ series1 step cc num1 _ series2 step cc num2 _ series1 count cc num2 _ series2 count cc series1 series {script_guid} xs cc series2 series {script_guid} ys cc {script_guid} surface panel1 _'
        )

        print(responses2)
