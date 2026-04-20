import sys
sys.path.append('..')
from ghclient import GHClient
import json


def send_command(port, ljson_type, command_name, params):
    """发送命令到GrasshopperSever"""
    data = {
        "name": ljson_type,
        "info": f"执行{command_name}命令",
        "value": {
            "Command": command_name,
            **params
        }
    }
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_command(**data)
        print(responses)
    return responses


def test_run_script(port=9988):
    """测试运行Rhino脚本"""
    print("=" * 50)
    print("测试1: 运行Rhino脚本")
    print("=" * 50)

    script = "_-CommandEcho _None"
    print(f"执行脚本: {script}")

    return send_command(port, "RHINO", "RHINOSCRIPT", {
        "Script": script
    })



def test_get_last_created_objects(port=9988):
    """测试获取最后创建的对象"""
    print("\n" + "=" * 50)
    print("测试2: 获取最后创建的对象")
    print("=" * 50)

    return send_command(port, "RHINO", "GETLASTCREATEDOBJECTS", {})



def test_select_objects(port=9988):
    """测试选择对象"""
    print("\n" + "=" * 50)
    print("测试3: 选择对象")
    print("=" * 50)

    test_guids = "00000000-0000-0000-0000-000000000000"
    print(f"尝试选择对象GUID: {test_guids}")

    return send_command(port, "RHINO", "SELECTOBJECTS", {
        "Objects": test_guids
    })



def test_create_point(port=9988):
    """测试创建一个点并获取最后创建的对象"""
    print("\n" + "=" * 50)
    print("测试4: 创建点并获取对象")
    print("=" * 50)

    print("步骤1: 创建点")
    create_script = "_-Point 0,0,0"

    results = send_command(port, "RHINO", "RHINOSCRIPT", {
        "Script": create_script
    })
    print(results)

    print("\n步骤2: 获取最后创建的对象")
    results = send_command(port, "RHINO", "GETLASTCREATEDOBJECTS", {})

    print(results)
    return results


if __name__ == "__main__":
    print("GrasshopperSever Rhino命令测试（端口9988）")
    print("=" * 50)

    test_run_script()
    test_get_last_created_objects()
    test_select_objects()
    test_create_point()

    print("\n" + "=" * 50)
    print("测试完成!")
    print("=" * 50)
