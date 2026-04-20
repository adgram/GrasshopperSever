"""
测试 SETPARAMVALUE - 设置组件值
端口：9653
"""

from ghclient import GHClient
import time

PORT = 9653

try:
    with GHClient(port = PORT) as client:
        time.sleep(0.5)
        
        # Panel 的 InstanceGuid（从上次测试获取）
        panel_guid = "4d59aa20-4f1e-4bb0-a8c4-c486d0ba571f"
        
        # 步骤 1: 设置 Panel 的文本值
        print("\n=== 步骤 1: 设置 Panel 的值为 'Hello Grasshopper' ===")
        r1 = client.send_command(
            name="Design",
            info="SETPARAMVALUE 测试",
            value={
                "Command": "SETPARAMVALUE",
                "InstanceGuid": panel_guid,
                "Value": "Hello Grasshopper"
            }
        )
        print(f"响应：{r1}")
        
        time.sleep(1)
        
        # 步骤 2: 重新添加 Number Slider
        print("\n=== 步骤 2: 重新添加 Number Slider ===")
        r2 = client.send_command(
            name="Design",
            info="SETPARAMVALUE 测试",
            value={
                "Command": "ADDCOMPONENTBYGUID",
                "ComponentGuid": "57da07bd-ecab-415d-9d86-af36d7073abc",
                "X": 300,
                "Y": 200
            }
        )
        print(f"响应：{r2}")
        
        time.sleep(2)
        
        # 步骤 3: 如果 Number Slider 添加成功，设置其值
        # 这里需要获取新添加的 Number Slider 的 InstanceGuid
        # 由于无法直接获取，我们假设用户提供
        print("\n=== 步骤 3: 等待 Number Slider 添加完成 ===")
        print("请在 Grasshopper 中查看 Number Slider 的 InstanceGuid")
        
except Exception as e:
    print(f"错误：{e}")
