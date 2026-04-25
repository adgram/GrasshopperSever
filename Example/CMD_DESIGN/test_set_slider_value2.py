"""
直接测试设置 Number Slider 的值
"""

from ghclient import GHClient
import time

PORT = 9653

try:
    with GHClient(port = PORT) as client:
        time.sleep(0.5)
        
        responses = client.send_command(
            name="Design",
            info="ADDCOMPONENTBYNAME 测试",
            value={
                "Command": "AddComponentByName",
                "ComponentName": "Number Slider",
                "X": 200,
                "Y": 100,
                "USERNICK": "Slider1"
            }
        )

        # 测试 1: 设置为 0.75
        print("设置 Number Slider 值为 0.75...")
        responses1 = client.send_command(
            name="Design",
            info="测试",
            value={
                "Command": "SETPARAMVALUE",
                "USERNICK": "Slider1",
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
                "USERNICK": "Slider1",
                "Value": "0 < 50< 67"
            }
        )
        
        if responses2:
            print(f"响应：{responses2}")
        else:
            print("未收到响应")
            
except Exception as e:
    print(f"错误：{e}")
