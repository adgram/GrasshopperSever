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

# 测试RUNSCRIPT命令
print("测试RUNSCRIPT命令（端口6655）")
print("=" * 50)

script = "_-Line 0,0,0 10,10,0"
print(f"执行脚本: {script}")

results = send_command(6655, "RHINO", "RUNSCRIPT", {
    "Script": script
})

print(f"收到 {len(results)} 条响应:")
for i, result in enumerate(results):
    print(f"\n响应 {i+1}:")
    print(f"  Name: {result.get('Name')}")
    print(f"  Info: {result.get('Info')}")
    print(f"  Value: {result.get('Value')}")

print("\n测试完成!")