"""
获取数据库路径
"""
from ghclient import GHClient

# 获取数据库路径
data = {
    'Name': 'DOCUMENT',
    'Info': '获取数据库路径',
    'Value': {'Command': 'DATABASEPATH'}
}

with GHClient(6879) as client:
    responses = client.send_command(**data)
    print(responses)