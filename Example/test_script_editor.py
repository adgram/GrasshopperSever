import socket
import json
from datetime import datetime

def send_data(port):
    """发送ScriptEditor数据"""
    data = {
        "Name": "ScriptEditor",
        "Info": "测试GHPython3代码执行",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Code": "import Rhino.Geometry as rg\nimport math\n\n# 创建一个点\nx = 10.0\ny = 20.0\nz = 30.0\n\npoint = rg.Point3d(x, y, z)\n\n# 返回点\na = point"
        }
    }

    try:
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        client.connect(('127.0.0.1', port))
        message = json.dumps(data, ensure_ascii=False)
        client.sendall((message + '\n').encode('utf-8'))

        print(f"已发送数据到端口 {port}:")
        print(f"  Name: {data['Name']}")
        print(f"  Info: {data['Info']}")
        print(f"  Value: {data['Value']}")

        # 接收响应
        client.settimeout(10)
        total_response = b''
        while True:
            try:
                chunk = client.recv(4096)
                if not chunk:
                    break
                total_response += chunk
            except socket.timeout:
                break

        client.close()

        # 解析响应
        if total_response:
            response = total_response.decode('utf-8-sig')
            messages = [msg for msg in response.split('\ufeff') if msg.strip()]
            results = [json.loads(msg.strip()) for msg in messages]

            print(f"\n收到 {len(results)} 条响应:")
            for i, result in enumerate(results):
                print(f"\n响应 {i+1}:")
                print(f"  Name: {result.get('Name')}")
                print(f"  Info: {result.get('Info')}")
                print(f"  Value: {result.get('Value')}")
            return results
        else:
            print("\n未收到响应")
            return []

    except Exception as e:
        print(f"连接失败: {e}")
        return []

if __name__ == "__main__":
    print("ScriptEditor测试")
    print("=" * 50)

    send_data(6895)

    print("\n" + "=" * 50)
    print("测试完成!")