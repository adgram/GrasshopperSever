"""
测试端口 5695 (示例中使用的端口)
"""

import socket
import json
from datetime import datetime
import time

HOST = '127.0.0.1'
PORT = 5695  # 注意：这里不是 9653

def send(client, name, value):
    data = {
        'Name': name,
        'Info': '端口测试',
        'Time': datetime.now().isoformat(),
        'Value': value
    }
    message = json.dumps(data, ensure_ascii=False)
    print(f"\n发送: {message}")
    client.sendall((message + '\n').encode('utf-8'))

def receive(client, timeout=3):
    client.settimeout(timeout)
    total = b''
    try:
        while True:
            chunk = client.recv(8192)
            if not chunk:
                break
            total += chunk
            time.sleep(0.2)
    except:
        pass
    if total:
        resp = total.decode('utf-8-sig')
        print(f"响应: {resp}")
        return resp
    return ""

print("连接端口 5695...")
client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

try:
    client.connect((HOST, PORT))
    print("连接成功!")
    receive(client, 2)
    time.sleep(0.5)

    # 添加 Panel
    print("\n添加 Panel 组件...")
    send(client, 'Design', {
        'Command': 'AddComponentByName',
        'ComponentName': 'Panel',
        'X': 100,
        'Y': 100
    })
    receive(client, 5)

    # 添加 Number Slider
    print("\n添加 Number Slider 组件...")
    send(client, 'Design', {
        'Command': 'AddComponentByName',
        'ComponentName': 'Number Slider',
        'X': 300,
        'Y': 100
    })
    receive(client, 5)

except ConnectionRefusedError:
    print(f"连接被拒绝，端口 {PORT} 可能未监听")
except Exception as e:
    print(f"错误: {e}")
finally:
    client.close()

print("\n请检查 Grasshopper 画布")
