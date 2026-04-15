"""
测试使用正确的原生 Panel GUID
"""

import socket
import json
from datetime import datetime
import time

HOST = '127.0.0.1'
PORT = 9653

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect((HOST, PORT))

client.settimeout(2)
try:
    resp = client.recv(8192)
    print(f"连接响应: {resp.decode('utf-8-sig')}")
except:
    pass

time.sleep(0.5)

# 使用原生 Panel 的 GUID
data = {
    'Name': 'Design',
    'Info': '测试',
    'Time': datetime.now().isoformat(),
    'Value': {
        'Command': 'ADDCOMPONENTBYGUID',
        'ComponentGuid': '59e0b89a-e487-49f8-bab8-b5bab16be14c',
        'X': 200,
        'Y': 200
    }
}

message = json.dumps(data, ensure_ascii=False)
print(f"\n发送: {message}")
client.sendall((message + '\n').encode('utf-8'))

client.settimeout(5)
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
    print(f"响应: {total.decode('utf-8-sig')}")
else:
    print("未收到响应")

client.close()
print("\n请检查画布上是否出现了原生 Panel 组件")
