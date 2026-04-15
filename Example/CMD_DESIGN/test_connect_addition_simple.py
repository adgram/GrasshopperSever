"""
测试 5: CONNECTCOMPONENTS - 连接两个 Addition 组件 (简化版)
端口：9653

测试流程:
1. 添加第一个 Addition 组件 (a0d62394-a118-422d-abb3-6af115c75b25)
2. 添加第二个 Addition 组件 (30d58600-1aab-42db-80a3-f1ea6c4269a0)
3. 连接第一个 Addition 的输出到第二个 Addition 的输入
"""

import socket
import json
import time
import re

HOST = '127.0.0.1'
PORT = 9653

# 指定的组件 GUID
ADDITION_GUID_1 = 'a0d62394-a118-422d-abb3-6af115c75b25'
ADDITION_GUID_2 = '30d58600-1aab-42db-80a3-f1ea6c4269a0'

def send_and_receive(command_dict, timeout=10):
    """发送命令并接收响应"""
    client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    client.settimeout(5)
    
    try:
        client.connect((HOST, PORT))
        time.sleep(0.5)
        
        # 发送
        data = {
            'Name': 'Design',
            'Info': 'CONNECTCOMPONENTS 测试 5',
            'Time': time.strftime('%Y-%m-%dT%H:%M:%S'),
            'Value': command_dict
        }
        message = json.dumps(data, ensure_ascii=False)
        print(f"[发送] {command_dict.get('Command', 'Unknown')}")
        client.sendall((message + '\n').encode('utf-8'))
        
        # 接收
        client.settimeout(timeout)
        total = b''
        start = time.time()
        while time.time() - start < timeout:
            try:
                chunk = client.recv(8192)
                if not chunk:
                    break
                total += chunk
                time.sleep(0.1)
            except socket.timeout:
                break
        
        if total:
            response = total.decode('utf-8-sig')
            print(f"[响应] {response[:300]}...")
            return response
        return ""
    except Exception as e:
        print(f"[错误] {e}")
        return ""
    finally:
        client.close()

def extract_guid(response):
    """从响应中提取 InstanceGuid"""
    if not response:
        return None
    
    response = response.replace('\ufeff', '').strip()
    
    # 先尝试从内层 JSON 解析（组件添加成功的响应）
    lines = response.split('\n')
    for line in lines:
        line = line.strip()
        if '组件添加成功' in line:
            # 提取转义的 JSON 部分
            match = re.search(r'组件添加成功(.+)', line)
            if match:
                json_str = match.group(1)
                # 查找 InstanceGuid
                guid_match = re.search(r'\\"InstanceGuid\\":\s*\\"([^"]+)\\"', json_str)
                if guid_match:
                    return guid_match.group(1)
    
    # 查找转义的 InstanceGuid（通用）
    matches_escaped = re.findall(r'\\"InstanceGuid\\":\s*\\"([^"]+)\\"', response)
    if matches_escaped:
        return matches_escaped[-1]
    
    # 查找未转义的
    matches = re.findall(r'"InstanceGuid"\s*:\s*"([^"]+)"', response)
    if matches:
        return matches[-1]
    
    return None

def main():
    print("="*60)
    print("GrasshopperSever CONNECTCOMPONENTS 测试 5")
    print("连接两个 Addition 组件")
    print("="*60)
    
    # 步骤 1: 添加第一个 Addition
    print("\n步骤 1: 添加第一个 Addition 组件")
    r1 = send_and_receive({
        'Command': 'ADDCOMPONENTBYGUID',
        'ComponentGuid': ADDITION_GUID_1,
        'X': 100,
        'Y': 100
    }, timeout=10)
    
    guid1 = extract_guid(r1)
    if guid1:
        print(f"[OK] Addition1 InstanceGuid: {guid1}")
    else:
        print(f"[失败] 未获取到 GUID1")
        print(f"  响应：{r1[:200]}")
        return
    
    time.sleep(1)
    
    # 步骤 2: 添加第二个 Addition
    print("\n步骤 2: 添加第二个 Addition 组件")
    r2 = send_and_receive({
        'Command': 'ADDCOMPONENTBYGUID',
        'ComponentGuid': ADDITION_GUID_2,
        'X': 300,
        'Y': 100
    }, timeout=10)
    
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
    r3 = send_and_receive({
        'Command': 'CONNECTCOMPONENTS',
        'FromGuid': guid1,
        'FromParameter': 'Result',
        'ToGuid': guid2,
        'ToParameter': 'First Number'
    }, timeout=10)
    
    print("\n" + "="*60)
    print("测试结果")
    print("="*60)
    print(f"Addition1: {guid1}")
    print(f"Addition2: {guid2}")
    print(f"连接响应：{r3[:200] if r3 else '空'}")
    print("\n请检查 Grasshopper 画布上两个组件之间是否有连线")

if __name__ == '__main__':
    main()
