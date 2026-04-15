"""
获取数据库路径
"""

import socket
import json
from datetime import datetime

HOST = '127.0.0.1'
PORT = 9653

client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
client.connect((HOST, PORT))

# 获取数据库路径
data = {
    'Name': 'DOCUMENT',
    'Info': '获取数据库路径',
    'Time': datetime.now().isoformat(),
    'Value': {'Command': 'DATABASEPATH'}
}

message = json.dumps(data, ensure_ascii=False)
print(f"发送: {message}")
client.sendall((message + '\n').encode('utf-8'))

client.settimeout(3)
try:
    response = client.recv(8192)
    print(f"响应: {response.decode('utf-8')}")
except:
    print("未收到响应")

client.close()
