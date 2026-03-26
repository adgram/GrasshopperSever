import socket
import json
from datetime import datetime

def send_command(port, ljson_type, command_name, params):
    """发送命令到GrasshopperSever"""
    data = {
        "Name": ljson_type,
        "Info": f"执行{command_name}命令",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Command": command_name,
            **params
        }
    }

    try:
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client.connect(('127.0.0.1', port))
        message = json.dumps(data, ensure_ascii=False)
        client.sendall((message + '\n').encode('utf-8'))

        client.settimeout(10)
        total_response = b''
        while True:
            try:
                chunk = client.recv(4096)
                if not chunk:
                    break
                total_response += chunk
            except socket.timeout:
                break

        client.close()

        if total_response:
            response = total_response.decode('utf-8-sig')
            messages = [msg for msg in response.split('\ufeff') if msg.strip()]
            results = [json.loads(msg.strip()) for msg in messages]
            return results
        return []

    except Exception as e:
        print(f"连接失败: {e}")
        return []

def test_run_script():
    """测试运行Rhino脚本"""
    print("=" * 50)
    print("测试1: 运行Rhino脚本")
    print("=" * 50)

    # 测试一个简单的Rhino命令
    script = "_-CommandEcho _None"
    print(f"执行脚本: {script}")

    results = send_command(9988, "RHINO", "RUNSCRIPT", {
        "Script": script
    })

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

def test_get_last_created_objects():
    """测试获取最后创建的对象"""
    print("\n" + "=" * 50)
    print("测试2: 获取最后创建的对象")
    print("=" * 50)

    results = send_command(9988, "RHINO", "GETLASTCREATEDOBJECTS", {})

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

def test_select_objects():
    """测试选择对象"""
    print("\n" + "=" * 50)
    print("测试3: 选择对象")
    print("=" * 50)

    # 尝试选择一些对象（如果没有对象，会返回错误）
    test_guids = "00000000-0000-0000-0000-000000000000"
    print(f"尝试选择对象GUID: {test_guids}")

    results = send_command(9988, "RHINO", "SELECTOBJECTS", {
        "Objects": test_guids
    })

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

def test_create_point():
    """测试创建一个点并获取最后创建的对象"""
    print("\n" + "=" * 50)
    print("测试4: 创建点并获取对象")
    print("=" * 50)

    # 先创建一个点
    print("步骤1: 创建点")
    create_script = "_-Point 0,0,0"
    results = send_command(9988, "RHINO", "RUNSCRIPT", {
        "Script": create_script
    })

    print(f"创建点 - 收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        if result.get('Name') not in ['OK']:
            print(f"  Name: {result.get('Name')}")
            print(f"  Value: {result.get('Value')}")

    # 然后获取最后创建的对象
    print("\n步骤2: 获取最后创建的对象")
    results = send_command(9988, "RHINO", "GETLASTCREATEDOBJECTS", {})

    print(f"获取对象 - 收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

if __name__ == "__main__":
    print("GrasshopperSever Rhino命令测试（端口9988）")
    print("=" * 50)

    # 测试运行Rhino脚本
    test_run_script()

    # 测试获取最后创建的对象
    test_get_last_created_objects()

    # 测试选择对象
    test_select_objects()

    # 测试创建点并获取
    test_create_point()

    print("\n" + "=" * 50)
    print("测试完成!")
    print("=" * 50)
