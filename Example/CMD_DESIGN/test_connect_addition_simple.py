"""
测试 5: CONNECTCOMPONENTS - 连接两个 Addition 组件 (简化版)
端口：9653

测试流程:
1. 添加第一个 Addition 组件 (a0d62394-a118-422d-abb3-6af115c75b25)
2. 添加第二个 Addition 组件 (30d58600-1aab-42db-80a3-f1ea6c4269a0)
3. 连接第一个 Addition 的输出到第二个 Addition 的输入
"""

from ghclient import GHClient
import time
import re

PORT = 9653

# 指定的组件 GUID
ADDITION_GUID_1 = 'a0d62394-a118-422d-abb3-6af115c75b25'
ADDITION_GUID_2 = '30d58600-1aab-42db-80a3-f1ea6c4269a0'

def extract_guid(response):
    """从响应中提取 InstanceGuid"""
    if not response:
        return None
    
    # 遍历响应列表
    for resp in response:
        value = resp.get('Value', '')
        if isinstance(value, str) and 'InstanceGuid' in value:
            # 查找转义的 InstanceGuid（通用）
            matches_escaped = re.findall(r'\\"InstanceGuid\\":\s*\\"([^"]+)\\"', value)
            if matches_escaped:
                return matches_escaped[-1]
            
            # 查找未转义的
            matches = re.findall(r'"InstanceGuid"\s*:\s*"([^"]+)"', value)
            if matches:
                return matches[-1]
    
    return None

def main():
    print("="*60)
    print("GrasshopperSever CONNECTCOMPONENTS 测试 5")
    print("连接两个 Addition 组件")
    print("="*60)
    
    try:
        with GHClient(port = PORT) as client:
            time.sleep(0.5)
            
            # 步骤 1: 添加第一个 Addition
            print("\n步骤 1: 添加第一个 Addition 组件")
            r1 = client.send_command(
                name="Design",
                info="CONNECTCOMPONENTS 测试 5",
                value={
                    "Command": "ADDCOMPONENTBYGUID",
                    "ComponentGuid": ADDITION_GUID_1,
                    "X": 100,
                    "Y": 100
                }
            )
            print(f"[响应] {r1[:300] if r1 else '空'}...")
            
            guid1 = extract_guid(r1)
            if guid1:
                print(f"[OK] Addition1 InstanceGuid: {guid1}")
            else:
                print(f"[失败] 未获取到 GUID1")
                print(f"  响应：{r1[:200] if r1 else '空'}")
                return
            
            time.sleep(1)
            
            # 步骤 2: 添加第二个 Addition
            print("\n步骤 2: 添加第二个 Addition 组件")
            r2 = client.send_command(
                name="Design",
                info="CONNECTCOMPONENTS 测试 5",
                value={
                    "Command": "ADDCOMPONENTBYGUID",
                    "ComponentGuid": ADDITION_GUID_2,
                    "X": 300,
                    "Y": 100
                }
            )
            print(f"[响应] {r2[:300] if r2 else '空'}...")
            
            guid2 = extract_guid(r2)
            if guid2:
                print(f"[OK] Addition2 InstanceGuid: {guid2}")
            else:
                print(f"[失败] 未获取到 GUID2")
                print(f"  响应：{r2[:200] if r2 else '空'}")
                return
            
            time.sleep(1)
            
            # 步骤 3: 连接两个组件
            print("\n步骤 3: 连接 Addition1 -> Addition2")
            r3 = client.send_command(
                name="Design",
                info="CONNECTCOMPONENTS 测试 5",
                value={
                    "Command": "CONNECTCOMPONENTS",
                    "FromGuid": guid1,
                    "FromParameter": "Result",
                    "ToGuid": guid2,
                    "ToParameter": "First Number"
                }
            )
            print(f"[响应] {r3[:300] if r3 else '空'}...")
            
            print("\n" + "="*60)
            print("测试结果")
            print("="*60)
            print(f"Addition1: {guid1}")
            print(f"Addition2: {guid2}")
            print(f"连接响应：{r3[:200] if r3 else '空'}")
            print("\n请检查 Grasshopper 画布上两个组件之间是否有连线")
            
    except Exception as e:
        print(f"[错误] {e}")

if __name__ == '__main__':
    main()
