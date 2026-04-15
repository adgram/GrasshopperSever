"""
单独测试移除 Number Slider
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

slider_guid = "9e2f18ed-0d94-4648-a81b-14084b528863"

print(f"移除 Number Slider (InstanceGuid: {slider_guid})...")
data = {
    'Name': 'Design',
    'Info': '测试',
    'Time': datetime.now().isoformat(),
    'Value': {
        'Command': 'REMOVECOMPONENT',
        'InstanceGuid': slider_guid
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

client.close()
