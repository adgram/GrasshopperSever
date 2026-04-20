from ghclient import GHClient

if __name__ == "__main__":
    print("ScriptEditor测试")
    print("=" * 50)

    data = {
        "name": "ScriptEditor",
        "info": "测试GHPython3代码执行",
        "value": {
            "OUTPUT": "import Rhino.Geometry as rg\nimport math\n\n# 创建一个点\nx = 10.0\ny = 20.0\nz = 30.0\n\npoint = rg.Point3d(x, y, z)\n\n# 返回点\na = point"
        }
    }
    with GHClient(port=6655) as client:
        responses = client.send_command(**data)
        print(responses)
    print("\n" + "=" * 50)
    print("测试完成!")