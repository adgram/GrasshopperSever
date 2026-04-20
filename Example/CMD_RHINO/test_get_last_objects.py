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

def test_create_object_and_get():
    """测试创建对象并获取最后创建的对象"""
    print("=" * 60)
    print("测试GETLASTCREATEDOBJECTS命令（端口6655）")
    print("=" * 60)

    # 步骤1: 创建一个对象
    print("步骤1: 创建一个点对象")
    create_script = "_-Point 5,5,0"
    print(f"执行脚本: {create_script}")

    results = send_command(6655, "RHINO", "RHINOSCRIPT", {
        "Script": create_script
    })

    print(f"创建对象 - 收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        if result.get('Name') not in ['OK']:
            print(f"  响应 {i+1}:")
            print(f"    Name: {result.get('Name')}")
            print(f"    Info: {result.get('Info')}")
            print(f"    Value: {result.get('Value')}")

    # 步骤2: 获取最后创建的对象
    print("\n步骤2: 获取最后创建的对象")
    results = send_command(6655, "RHINO", "GETLASTCREATEDOBJECTS", {})

    print(f"获取对象 - 收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {json.dumps(result.get('Value'), indent=2, ensure_ascii=False)}")

    return results

def test_get_objects_without_creation():
    """测试直接获取对象（不创建新对象）"""
    print("\n" + "=" * 60)
    print("测试直接获取对象（不创建新对象）")
    print("=" * 60)

    results = send_command(6655, "RHINO", "GETLASTCREATEDOBJECTS", {})

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {json.dumps(result.get('Value'), indent=2, ensure_ascii=False)}")

    return results

if __name__ == "__main__":
    print("GETLASTCREATEDOBJECTS 命令测试")
    print("=" * 60)
    # 测试创建对象并获取最后创建的对象
    test_create_object_and_get()
    # 测试直接获取对象（不创建新对象）
    test_get_objects_without_creation()

    print("\n" + "=" * 60)
    print("测试完成!")
    print("=" * 60)