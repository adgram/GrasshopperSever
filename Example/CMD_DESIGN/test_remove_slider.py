"""
单独测试移除 Number Slider
"""

from ghclient import GHClient
import time

PORT = 9653

slider_guid = "9e2f18ed-0d94-4648-a81b-14084b528863"

try:
    with GHClient(port = PORT) as client:
        time.sleep(0.5)
        
        print(f"移除 Number Slider (InstanceGuid: {slider_guid})...")
        responses = client.send_command(
            name="Design",
            info="测试",
            value={
                "Command": "REMOVECOMPONENT",
                "InstanceGuid": slider_guid
            }
        )
        
        if responses:
            print(f"响应：{responses}")
        else:
            print("未收到响应")
            
except Exception as e:
    print(f"错误：{e}")
