"""
测试 SETCOMPONENTVALUE - 设置组件值
端口: 9653
"""

import socket
import json
from datetime import datetime
import time

HOST = '127.0.0.1'
PORT = 9653

def send(client, name, value):
    data = {
        'Name': name,
        'Info': 'SETCOMPONENTVALUE测试',
        'Time': datetime.now().isoformat(),
        'Value': value
    }
    message = json.dumps(data, ensure_ascii=False)
    print(f"\n发送: {message}")
    client.sendall((message + '\n').encode('utf-8'))

def receive(client, timeout=5):
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

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect((HOST, PORT))
receive(client, 2)
time.sleep(0.5)

# Panel 的 InstanceGuid（从上次测试获取）
panel_guid = "4d59aa20-4f1e-4bb0-a8c4-c486d0ba571f"

# 步骤1: 设置 Panel 的文本值
print("\n=== 步骤1: 设置 Panel 的值为 'Hello Grasshopper' ===")
send(client, 'Design', {
    'Command': 'SETCOMPONENTVALUE',
    'InstanceGuid': panel_guid,
    'Value': 'Hello Grasshopper'
})
r1 = receive(client)

time.sleep(1)

# 步骤2: 重新添加 Number Slider
print("\n=== 步骤2: 重新添加 Number Slider ===")
send(client, 'Design', {
    'Command': 'ADDCOMPONENTBYGUID',
    'ComponentGuid': '57da07bd-ecab-415d-9d86-af36d7073abc',
    'X': 300,
    'Y': 200
})
r2 = receive(client)

time.sleep(2)

# 步骤3: 如果 Number Slider 添加成功，设置其值
# 这里需要获取新添加的 Number Slider 的 InstanceGuid
# 由于无法直接获取，我们假设用户手动提供
print("\n=== 步骤3: 等待 Number Slider 添加完成 ===")
print("请在 Grasshopper 中查看 Number Slider 的 InstanceGuid")

client.close()
