"""
测试创建环状体并在Grasshopper中拾取
端口：5695
"""
import sys
sys.path.append('..')
from ghclient import GHClient
import json
import time


def send_command(port, ljson_type, command_name, params):
    """发送命令到GrasshopperSever（标准方法）"""
    data = {
        "name": ljson_type,
        "info": f"执行{command_name}命令",
        "value": {
            "Command": command_name,
            **params
        }
    }
    responses = []
    with GHClient(port=port) as client:
        responses = client.send_command(**data)
        print(responses)
    return responses


def create_torus_and_pick():
    """创建环状体并在Grasshopper中拾取"""
    print("=" * 60)
    print("测试：创建环状体并在Grasshopper中拾取")
    print("=" * 60)

    PORT = 5695

    # 步骤1: 在Rhino中创建一个环状体
    print("\n步骤1: 在Rhino中创建环状体")
    # Torus命令格式: _-Torus [中心点] [半径] [管半径]
    # 创建一个中心在原点，半径10，管半径2的环状体
    torus_script = "_-Torus 0,0,0 10 2"
    print(f"执行脚本: {torus_script}")

    responses = send_command(PORT, "RHINO", "RHINOSCRIPT", {
        "Script": torus_script
    })

    print(f"创建环状体 - 收到 {len(responses)} 条响应")
    for i, response in enumerate(responses):
        print(f"  响应 {i+1}: {response.get('Name')} - {response.get('Info')}")

    # 等待更长时间确保对象创建完成
    print("  等待对象创建完成...")
    time.sleep(2)

    # 步骤2: 获取最后创建的对象的guid
    print("\n步骤2: 获取最后创建的对象的guid")
    responses = send_command(PORT, "RHINO", "GETLASTCREATEDOBJECTS", {})

    print(f"获取对象 - 收到 {len(responses)} 条响应")

    torus_guid = None
    for i, response in enumerate(responses):
        if response.get('Name') == 'GetLastCreatedObjects':
            value = response.get('Value', {})
            # 查找Object_0字段
            if 'Object_0' in value:
                torus_guid = value['Object_0']['Id']
                print(f"  ✅ 找到环状体GUID: {torus_guid}")
                print(f"  类型: {value['Object_0']['Type']}")
                print(f"  图层: {value['Object_0']['Layer']}")
                break
        print(f"  响应 {i+1}: {response.get('Name')} - {response.get('Info')}")

    if not torus_guid:
        print("❌ 未能获取到环状体的GUID")
        return

    # 步骤3: 在Grasshopper中创建一个brep参数并拾取该环状体
    print("\n步骤3: 在Grasshopper中创建brep参数并拾取环状体")
    print(f"  参数类型: brep")
    print(f"  位置: (100, 100)")
    print(f"  GUID值: {torus_guid}")

    responses = send_command(PORT, "Design", "AddParamWithValue", {
        "ParamName": "brep",
        "X": 100,
        "Y": 100,
        "Value": torus_guid
    })

    print(f"添加参数 - 收到 {len(responses)} 条响应")
    for i, response in enumerate(responses):
        print(f"  响应 {i+1}:")
        print(f"    Name: {response.get('Name')}")
        print(f"    Info: {response.get('Info')}")
        if response.get('Value'):
            print(f"    Value: {json.dumps(response.get('Value'), indent=4, ensure_ascii=False)}")

    print("\n" + "=" * 60)
    print("✅ 测试完成！")
    print(f"环状体已创建，GUID: {torus_guid}")
    print("已在Grasshopper中创建brep参数并拾取该环状体")
    print("=" * 60)


if __name__ == "__main__":
    create_torus_and_pick()