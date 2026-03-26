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

def test_load_saved_document():
    """测试加载刚才保存的文档"""
    print("=" * 50)
    print("测试: 加载刚才保存的文档")
    print("=" * 50)

    load_path = r"C:\Users\SZAUPD\AppData\Roaming\Grasshopper\Libraries\GHserver\test\test_save.gh"
    print(f"加载文件: {load_path}")

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

def test_save_again():
    """再次保存文档（应该成功，因为已经保存过）"""
    print("\n" + "=" * 50)
    print("测试: 再次保存文档")
    print("=" * 50)

    results = send_command(6879, "DOCUMENT", "SAVEDOCUMENT", {})

    print(f"收到 {len(results)} 条响应:")
    for i, result in enumerate(results):
        print(f"\n响应 {i+1}:")
        print(f"  Name: {result.get('Name')}")
        print(f"  Info: {result.get('Info')}")
        print(f"  Value: {result.get('Value')}")

    return results

if __name__ == "__main__":
    print("Grasshopper文档完整测试")
    print("=" * 50)

    # 测试加载刚才保存的文档
    test_load_saved_document()

    # 测试再次保存
    test_save_again()

    print("\n" + "=" * 50)
    print("测试完成!")
    print("=" * 50)