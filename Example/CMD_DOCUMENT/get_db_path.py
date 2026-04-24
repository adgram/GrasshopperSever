"""
获取数据库路径
"""
from ghclient import GHClient

# 获取数据库路径
data = {
    'name': 'DOCUMENT',
    'info': '获取数据库路径',
    'value': {'Command': 'DATABASEPATH'}
}

with GHClient(port=6879) as client:
    responses = client.send_command(**data)
    print(responses)