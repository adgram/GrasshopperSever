"""
测试 5: CONNECTCOMPONENTS - 连接两个 组件 (简化版)
端口：9653

测试流程:
1. 添加第一个 Addition 组件
2. 添加第二个 Larger Than 组件
3. 连接第一个 Addition 的输出到第二个 Larger Than 的输入
"""

from ghclient import GHClient
import time

PORT = 9653

def main():
    print("="*60)
    print("GrasshopperSever CONNECTCOMPONENTS 测试 5")
    print("连接两个组件")
    print("="*60)
    
    try:
        with GHClient(port = PORT) as client:
            time.sleep(0.5)
            
            # 步骤 1: 添加第一个 Addition
            ps = client.send_command(
                name="Design",
                info="CONNECTCOMPONENTS 测试 6",
                value={
                    "Command": "ADDCOMPONENTBYNAME",
                    "ComponentName": "Addition",
                    "X": 100,
                    "Y": 100,
                    "USERNICK": "Add1"
                }
            )
            print(ps)
            client.send_command(
                name="Design",
                info="CONNECTCOMPONENTS 测试 6",
                value={
                    "Command": "ADDCOMPONENTBYNAME",
                    "ComponentName": "Larger Than",
                    "X": 500,
                    "Y": 100,
                    "USERNICK": "LargerThan1"
                }
            )

            time.sleep(1)
            
            # 步骤 3: 连接两个组件
            print("\n步骤 3: 连接 Addition -> LargerThan")
            r3 = client.send_command(
                name="Design",
                info="CONNECTCOMPONENTS 测试 5",
                value={
                    "Command": "CONNECTCOMPONENTS",
                    "FromNICK": "Add1",
                    "FromParameter": "Result",
                    "ToNICK": "LargerThan1",
                    "ToParameter": "First Number"
                }
            )
            print(f"[响应] {r3[:300] if r3 else '空'}...")
            
            print("\n" + "="*60)
            print("测试结果")
            print("="*60)
            print(f"连接响应：{r3[:200] if r3 else '空'}")
            print("\n请检查 Grasshopper 画布上两个组件之间是否有连线")
            
    except Exception as e:
        print(f"[错误] {e}")

if __name__ == '__main__':
    main()
