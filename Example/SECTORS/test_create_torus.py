"""
测试创建环状体并在Grasshopper中拾取
端口：5695
"""
from ghclient import GHClient


def create_torus_and_pick():
    """创建环状体并在Grasshopper中拾取"""
    print("=" * 60)
    print("测试：创建环状体并在Grasshopper中拾取")
    print("=" * 60)

    with GHClient(port=5695) as client:
        # 步骤1: 在Rhino中创建一个环状体
        print("\n步骤1: 在Rhino中创建环状体")
        # Torus命令格式: _-Torus [中心点] [半径] [管半径]
        # 创建一个中心在原点，半径10，管半径2的环状体
        torus_script = "_-Torus 0,0,0 10 2"
        print(f"执行脚本: {torus_script}")

        responses1 = client.send_command("RHINO", "", {
            "Command": "RHINOSCRIPT",
            "Script": torus_script
        })
        print(responses1)

        # 步骤2: 获取最后创建的对象的guid
        print("\n步骤2: 获取最后创建的对象的guid")
        responses2 = client.send_command("RHINO", "", {
            "Command": "GETLASTCREATEDOBJECTS"
        })
        print(responses2)
        torus_id = GHClient.extract_value(responses2, 'Object_0')['Id']
        if not torus_id:
            print("❌ 未能获取到环状体的GUID")
            return

        responses = client.send_command("Design", "", {
            "Command": "AddParamWithValue",
            "ParamName": "brep",
            "X": 100,
            "Y": 100,
            "Value": torus_id
        })
        print(responses)

    print("\n" + "=" * 60)
    print("✅ 测试完成！")
    print("已在Grasshopper中创建brep参数并拾取该环状体")
    print("=" * 60)


if __name__ == "__main__":
    create_torus_and_pick()