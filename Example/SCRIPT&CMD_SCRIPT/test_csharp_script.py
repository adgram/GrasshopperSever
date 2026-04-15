import socket
import json
from datetime import datetime

def send_data(port):
    """发送C# Script数据"""
    data = {
        "Name": "ScriptEditor",
        "Info": "测试C#代码执行",
        "Time": datetime.now().isoformat(),
        "Value": {
            "Code": "// Grasshopper Script Instance\n#region Usings\nusing System;\nusing System.Linq;\nusing System.Collections;\nusing System.Collections.Generic;\nusing System.Drawing;\n\nusing Rhino;\nusing Rhino.Geometry;\n\nusing Grasshopper;\nusing Grasshopper.Kernel;\nusing Grasshopper.Kernel.Data;\nusing Grasshopper.Kernel.Types;\n#endregion\n\npublic class Script_Instance : GH_ScriptInstance\n{\n    #region Notes\n    /* \n      Members:\n        RhinoDoc RhinoDocument\n        GH_Document GrasshopperDocument\n        IGH_Component Component\n        int Iteration\n\n      Methods (Virtual & overridable):\n        Print(string text)\n        Print(string format, params object[] args)\n        Reflect(object obj)\n        Reflect(object obj, string method_name)\n    */\n    #endregion\n\n    private void RunScript(object x, object y, ref object a)\n    {\n        // 创建一个点\n        Rhino.Geometry.Point3d point = new Rhino.Geometry.Point3d(10.0, 20.0, 30.0);\n        \n        // 返回点\n        a = point;\n    }\n}"
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
    print("C# Script测试")
    print("=" * 50)

    send_data(6895)

    print("\n" + "=" * 50)
    print("测试完成!")