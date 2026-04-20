"""
测试 REMOVECOMPONENT - 移除组件
端口：9653
"""

from ghclient import GHClient
import time

PORT = 9653

try:
    with GHClient(port = PORT) as client:
        time.sleep(0.5)
        
        # 之前创建的组件 InstanceGuid
        panel_guid = "4d59aa20-4f1e-4bb0-a8c4-c486d0ba571f"
        slider_guid = "9e2f18ed-0d94-4648-a81b-14084b528863"
        
        # 测试 1: 移除 Panel
        print("\n=== 测试 1: 移除 Panel 组件 ===")
        r1 = client.send_command(
            name="Design",
            info="REMOVECOMPONENT 测试",
            value={
                "Command": "REMOVECOMPONENT",
                "InstanceGuid": panel_guid
            }
        )
        print(f"响应：{r1}")
        
        time.sleep(1)
        
        # 测试 2: 移除 Number Slider
        print("\n=== 测试 2: 移除 Number Slider 组件 ===")
        r2 = client.send_command(
            name="Design",
            info="REMOVECOMPONENT 测试",
            value={
                "Command": "REMOVECOMPONENT",
                "InstanceGuid": slider_guid
            }
        )
        print(f"响应：{r2}")
        
except Exception as e:
    print(f"错误：{e}")

print("\n请检查画布上的两个组件是否被移除")
