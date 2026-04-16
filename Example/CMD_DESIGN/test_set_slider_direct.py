"""
直接测试设置 Number Slider 的值
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
    client.recv(8192)
except:
    pass

time.sleep(0.5)

slider_guid = "8210e72c-09e5-4f7e-af9d-acefc4e03870"

# 测试1: 设置为 0.75
print("设置 Number Slider 值为 0.75...")
data = {
    'Name': 'Design',
    'Info': '测试',
    'Time': datetime.now().isoformat(),
    'Value': {
        'Command': 'SETPARAMVALUE',
        'InstanceGuid': slider_guid,
        'Value': '0.75'
    }
}

client.sendall((json.dumps(data) + '\n').encode('utf-8'))

client.settimeout(5)
total = b''
try:
    while True:
        chunk = client.recv(8192)
        if not chunk:
            break
        total += chunk
        time.sleep(0.3)
except:
    pass

if total:
    print(f"响应: {total.decode('utf-8-sig')}")
else:
    print("未收到响应")

time.sleep(2)

# 测试2: 设置为 50
print("\n设置 Number Slider 值为 50...")
data2 = {
    'Name': 'Design',
    'Info': '测试',
    'Time': datetime.now().isoformat(),
    'Value': {
        'Command': 'SETPARAMVALUE',
        'InstanceGuid': slider_guid,
        'Value': '0 < 50< 67'
    }
}

client.sendall((json.dumps(data2) + '\n').encode('utf-8'))

total2 = b''
try:
    while True:
        chunk = client.recv(8192)
        if not chunk:
            break
        total2 += chunk
        time.sleep(0.3)
except:
    pass

if total2:
    print(f"响应: {total2.decode('utf-8-sig')}")
else:
    print("未收到响应")

client.close()
