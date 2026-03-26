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

def test_simple_commands():
    """测试更简单的Rhino命令"""
    print("=" * 50)
    print("测试简单的Rhino脚本命令")
    print("=" * 50)

    # 测试不同的Rhino命令
    test_scripts = [
        "Command",
        "Version",
        "Echo Test",
        "What"
    ]

    for script in test_scripts:
        print(f"\n测试命令: {script}")
        print("-" * 50)

        results = send_command(9988, "RHINO", "RUNSCRIPT", {
            "Script": script
        })

        print(f"收到 {len(results)} 条响应:")
        for i, result in enumerate(results):
            if result.get('Name') != 'OK':
                print(f"  Name: {result.get('Name')}")
                print(f"  Info: {result.get('Info')}")
                print(f"  Value: {result.get('Value')}")

if __name__ == "__main__":
    print("Rhino简单命令测试")
    print("=" * 50)

    test_simple_commands()

    print("\n" + "=" * 50)
    print("测试完成!")
    print("=" * 50)