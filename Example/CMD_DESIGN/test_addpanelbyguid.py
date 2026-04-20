"""
测试使用正确的原生 Panel GUID
"""

from ghclient import GHClient
import time

PORT = 9653

try:
    with GHClient(port = PORT) as client:
        time.sleep(0.5)
        
        # 使用原生 Panel 的 GUID
        responses = client.send_command(
            name="Design",
            info="测试",
            value={
                "Command": "ADDCOMPONENTBYGUID",
                "ComponentGuid": "59e0b89a-e487-49f8-bab8-b5bab16be14c",
                "X": 200,
                "Y": 200
            }
        )
        
        if responses:
            print(f"响应：{responses}")
        else:
            print("未收到响应")
            
except Exception as e:
    print(f"错误：{e}")

print("\n请检查画布上是否出现了原生 Panel 组件")
