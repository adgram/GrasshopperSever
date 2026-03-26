import socket
import json
from datetime import datetime

def send_command(port, ljson_type, command_name, params):
    """发送命令到GrasshopperSever

    Args:
        port: 端口号
        ljson_type: 命令类型 (COMPONENT/DOCUMENT/RHINO/SCRIPT/DESIGN)
        command_name: 具体命令名称
        params: 命令参数字典
    """
    data = {
        "Name": ljson_type,  # 命令类型
        "Info": f"执行{command_name}命令",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Command": command_name,  # 具体命令
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

        # 解析响应（可能有多个消息）
        if total_response:
            response = total_response.decode('utf-8-sig')
            messages = [msg for msg in response.split('\ufeff') if msg.strip()]
            results = [json.loads(msg.strip()) for msg in messages]
            return results
        return []

    except Exception as e:
        print(f"连接失败: {e}")
        return []

def test_save_document():
    """测试保存文档"""
    print("=" * 50)
    print("测试1: 保存当前文档")
    print("=" * 50)

    # 尝试保存当前文档（不指定路径，使用默认路径）
    results = send_command(6879, "DOCUMENT", "SAVEDOCUMENT", {})

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

def test_save_document_with_path():
    """测试保存文档到指定路径"""
    print("\n" + "=" * 50)
    print("测试2: 保存文档到指定路径")
    print("=" * 50)

    save_path = r"C:\Users\SZAUPD\AppData\Roaming\Grasshopper\Libraries\GHserver\test\test_save.gh"
    results = send_command(6879, "DOCUMENT", "SAVEDOCUMENT", {
        "FilePath": save_path
    })

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

def test_load_document():
    """测试加载文档"""
    print("\n" + "=" * 50)
    print("测试3: 加载文档")
    print("=" * 50)

    # 先检查是否有可用的gh文件
    import os
    test_dir = r"C:\Users\SZAUPD\AppData\Roaming\Grasshopper\Libraries\GHserver\test"
    gh_files = [f for f in os.listdir(test_dir) if f.endswith('.gh')]

    if gh_files:
        load_path = os.path.join(test_dir, gh_files[0])
        print(f"加载文件: {load_path}")
    else:
        print("测试目录中没有.gh文件，跳过加载测试")
        return []

    results = send_command(6879, "DOCUMENT", "LOADDOCUMENT", {
        "FilePath": load_path
    })

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

def test_database_path():
    """测试获取数据库路径"""
    print("\n" + "=" * 50)
    print("测试4: 获取数据库路径")
    print("=" * 50)

    results = send_command(6879, "DOCUMENT", "DATABASEPATH", {})

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

if __name__ == "__main__":
    print("Grasshopper文档API测试")
    print("=" * 50)

    # 测试获取数据库路径
    test_database_path()

    # 测试保存文档
    test_save_document()

    # 测试保存到指定路径
    test_save_document_with_path()

    # 测试加载文档
    test_load_document()

    print("\n" + "=" * 50)
    print("测试完成!")
    print("=" * 50)
