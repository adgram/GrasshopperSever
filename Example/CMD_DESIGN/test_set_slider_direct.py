"""
直接测试设置 Number Slider 的值
"""

from ghclient import GHClient
import time

PORT = 9653

slider_guid = "8210e72c-09e5-4f7e-af9d-acefc4e03870"

try:
    with GHClient(port = PORT) as client:
        time.sleep(0.5)
        
        # 测试 1: 设置为 0.75
        print("设置 Number Slider 值为 0.75...")
        responses1 = client.send_command(
            name="Design",
            info="测试",
            value={
                "Command": "SETPARAMVALUE",
                "InstanceGuid": slider_guid,
                "Value": "0.75"
            }
        )
        
        if responses1:
            print(f"响应：{responses1}")
        else:
            print("未收到响应")
        
        time.sleep(2)
        
        # 测试 2: 设置为 50
        print("\n设置 Number Slider 值为 50...")
        responses2 = client.send_command(
            name="Design",
            info="测试",
            value={
                "Command": "SETPARAMVALUE",
                "InstanceGuid": slider_guid,
                "Value": "0 < 50< 67"
            }
        )
        
        if responses2:
            print(f"响应：{responses2}")
        else:
            print("未收到响应")
            
except Exception as e:
    print(f"错误：{e}")
