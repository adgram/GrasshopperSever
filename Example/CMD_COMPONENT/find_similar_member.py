"""
查询"Find similar member"组件的信息
端口：6655
"""
from ghclient import GHClient

if __name__ == '__main__':
    request = {
        "name": "COMPONENT",
        "info": "通过名称查找组件",
        "value": {
            "Command": "FINDCOMPONENTBYNAME",
            "Name": "Find similar member"
        }
    }
    with GHClient(port=6655) as client:
        responses = client.send_command(**request)
        print(responses)