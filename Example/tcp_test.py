"""
TCP 测试客户端 - 用于测试 GHServer 的连接和消息收发功能
"""
from ghclient import GHClient


def test_connection(port):
    """测试 1: 连接服务器 - 应收到 1 条消息"""
    client = GHClient(port=port)
    messages = client.connect()
    client.disconnect()
    return len(messages) == 1


def test_send_message(port):
    """测试 2: 发送字符串消息 - 应收到 1 条成功接收确认"""
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="String",
            info="字符串测试",
            value="Hello, GHServer!"
        )
        print(responses)
    return len(responses) == 1


def test_send_number(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="Number",
            info="数字测试",
            value=123.45
        )
        print(responses)
    return len(responses) == 1


def test_send_list(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="List",
            info="列表测试",
            value=[1, 2, 3, 4, 5]
        )
        print(responses)
    return len(responses) == 1


def test_send_dict(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="Dict",
            info="字典测试",
            value={"x": 10, "y": 20, "z": 30}
        )
        print(responses)
    return len(responses) == 1


def test_send_nested(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="Nested",
            info="嵌套测试",
            value={
                "type": "Point",
                "coordinates": [10, 20, 5]
            }
        )
        print(responses)
    return len(responses) == 1


def test_send_geometry(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="Geometry",
            info="几何数据",
            value=[
                {"type": "Point", "x": 0, "y": 0, "z": 0},
                {"type": "Point", "x": 10, "y": 20, "z": 5},
                {"type": "Point", "x": 20, "y": 10, "z": 10}
            ]
        )
        print(responses)
    return len(responses) == 1

def test_send_output(port):
    '''测试 8: 发送 OUTPUT 数据 - 应收到 1 条成功接收确认
    这里只有output字段被提取出来'''
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_msg(
            name="TestData",
            info="测试发送 OUTPUT 数据",
            value={
                "OUTPUT": "测试输出数据",
                "Message": "这是一条测试消息",
                "Number": 123.45
            }
        )
        print(responses)
    return len(responses) == 1

def test_command_document(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_command(
            name="DOCUMENT",
            info="获取数据库路径",
            value={"Command": "DATABASEPATH"}
        )
        print(responses)
    return len(responses) == 2


def test_command_rhino(port):
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_command(
            name="RHINO",
            info="Rhino 命令测试",
            value={
                "Command": "GETLASTCREATEDOBJECTS"
            }
        )
        print(responses)
    return len(responses) == 2



if __name__ == "__main__":
    port = 6655

    tests = [
        ("连接测试", test_connection),
        ("发送字符串", test_send_message),
        ("发送数字", test_send_number),
        ("发送列表", test_send_list),
        ("发送字典", test_send_dict),
        ("发送嵌套数据", test_send_nested),
        ("发送几何数据", test_send_geometry),
        ("发送 OUTPUT 数据", test_send_output),
        ("DOCUMENT 命令", test_command_document),
        ("RHINO 命令", test_command_rhino),
    ]

    print(f"开始测试 GHServer (端口 {port})")
    print("=" * 50)

    passed = 0
    failed = 0

    for name, test_func in tests:
        try:
            if test_func(port):
                passed += 1
                print(f"✓ {name}: 通过")
            else:
                failed += 1
                print(f"✗ {name}: 失败")
        except Exception as e:
            failed += 1
            print(f"✗ {name}: 错误 - {e}")

    print("=" * 50)
    print(f"总计：{passed} 通过，{failed} 失败")
