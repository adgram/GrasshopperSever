"""
添加 Number Slider 并获取 InstanceGuid，然后设置值
每次命令都重新连接
"""

from ghclient import GHClient
import time

PORT = 9653


def main():
    print("="*60)
    print("GrasshopperSever SETPARAMVALUE 测试")
    print("添加 Number Slider 并设置值")
    print("="*60)
    
    try:
        with GHClient(port = PORT) as client:
            time.sleep(0.5)
            
            # 步骤 1: 添加 Number Slider
            print("\n步骤 1: 添加 Number Slider 组件")
            r1 = client.send_command(
                name="Design",
                info="SETPARAMVALUE 测试",
                value={
                    "Command": "ADDCOMPONENTBYGUID",
                    "ComponentGuid": "57da07bd-ecab-415d-9d86-af36d7073abc",
                    "X": 500,
                    "Y": 100
                }
            )
            print(f"[响应] {r1[:300] if r1 else '空'}...")
            
            guid = client.extract_value(r1, "InstanceGuid")
            if guid:
                print(f"[OK] Number Slider InstanceGuid: {guid}")
            else:
                print(f"[失败] 未获取到 GUID")
                print(f"  响应：{r1[:200] if r1 else '空'}")
                return
            
            time.sleep(1)
            
            # 步骤 2: 设置值
            print("\n步骤 2: 设置 Number Slider 值为 0.75")
            r2 = client.send_command(
                name="Design",
                info="SETPARAMVALUE 测试",
                value={
                    "Command": "SETPARAMVALUE",
                    "InstanceGuid": guid,
                    "Value": "0.75"
                }
            )
            print(f"[响应] {r2[:300] if r2 else '空'}...")
            
            print("\n" + "="*60)
            print("测试完成")
            print("="*60)
            print(f"InstanceGuid: {guid}")
            print(f"设置值响应：{r2[:200] if r2 else '空'}")
            print("\n请检查 Grasshopper 画布上 Number Slider 的值是否为 0.75")
            
    except Exception as e:
        print(f"[错误] {e}")

if __name__ == '__main__':
    main()
