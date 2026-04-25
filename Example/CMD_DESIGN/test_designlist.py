"""
测试 批量操作
端口：9653
"""

from ghclient import GHClient

PORT = 9653

'''
"ac" => AddComponentByName(string name, string x, string y, string nick)
"ap" => AddParamWithValue(string name, string x, string y, string path, string Value, string nick)
"dp" => RemoveComponent(string nick)
"sv" => SetParamValue(string nick, string path, string value)
"cc" => ConnectComponents(string fromNick, string fromParam, string toNick, string toParam)
"dc" => DisconnectComponents(string fromNick, string fromParam, string toNick, string toParam)
'''

try:
    with GHClient(port = PORT) as client:
        responses = client.send_command(
            name="DesignList",
            info="批量操作测试",
            value='ac Addition 100 100 Add1 ac "Larger Than" 500 100 LargerThan1 cc Add1 Result LargerThan1 "First Number"'
        )
        print(f"响应：{responses}")
except Exception as e:
    print(f"错误：{e}")
