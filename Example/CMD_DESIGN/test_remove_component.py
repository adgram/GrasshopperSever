"""
测试 REMOVECOMPONENT - 移除组件
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
        'Info': 'REMOVECOMPONENT测试',
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

# 之前创建的组件 InstanceGuid
panel_guid = "4d59aa20-4f1e-4bb0-a8c4-c486d0ba571f"
slider_guid = "9e2f18ed-0d94-4648-a81b-14084b528863"

# 测试1: 移除 Panel
print("\n=== 测试1: 移除 Panel 组件 ===")
send(client, 'Design', {
    'Command': 'REMOVECOMPONENT',
    'InstanceGuid': panel_guid
})
r1 = receive(client)

time.sleep(1)

# 测试2: 移除 Number Slider
print("\n=== 测试2: 移除 Number Slider 组件 ===")
send(client, 'Design', {
    'Command': 'REMOVECOMPONENT',
    'InstanceGuid': slider_guid
})
r2 = receive(client)

client.close()
print("\n请检查画布上的两个组件是否被移除")
