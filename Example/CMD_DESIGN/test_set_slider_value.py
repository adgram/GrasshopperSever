"""
添加 Number Slider 并获取 InstanceGuid，然后设置值
每次命令都重新连接
"""

import socket
import json
import re
from datetime import datetime
import time

HOST = '127.0.0.1'
PORT = 9653

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
            'Info': 'SETCOMPONENTVALUE 测试',
            'Time': datetime.now().isoformat(),
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
                guid_match = re.search(r'\\"InstanceGuid\\":\s*\\"([^\"]+)\\"', json_str)
                if guid_match:
                    return guid_match.group(1)
    
    # 查找转义的 InstanceGuid（通用）
    matches_escaped = re.findall(r'\\"InstanceGuid\\":\s*\\"([^\"]+)\\"', response)
    if matches_escaped:
        return matches_escaped[-1]
    
    # 查找未转义的
    matches = re.findall(r'"InstanceGuid"\s*:\s*"([^"]+)"', response)
    if matches:
        return matches[-1]
    
    return None

def main():
    print("="*60)
    print("GrasshopperSever SETCOMPONENTVALUE 测试")
    print("添加 Number Slider 并设置值")
    print("="*60)
    
    # 步骤 1: 添加 Number Slider
    print("\n步骤 1: 添加 Number Slider 组件")
    r1 = send_and_receive({
        'Command': 'ADDCOMPONENTBYGUID',
        'ComponentGuid': '57da07bd-ecab-415d-9d86-af36d7073abc',
        'X': 500,
        'Y': 100
    }, timeout=10)
    
    guid = extract_guid(r1)
    if guid:
        print(f"[OK] Number Slider InstanceGuid: {guid}")
    else:
        print(f"[失败] 未获取到 GUID")
        print(f"  响应：{r1[:200]}")
        return
    
    time.sleep(1)
    
    # 步骤 2: 设置值
    print("\n步骤 2: 设置 Number Slider 值为 0.75")
    r2 = send_and_receive({
        'Command': 'SETCOMPONENTVALUE',
        'InstanceGuid': guid,
        'Value': '0.75'
    }, timeout=10)
    
    print("\n" + "="*60)
    print("测试完成")
    print("="*60)
    print(f"InstanceGuid: {guid}")
    print(f"设置值响应：{r2[:200] if r2 else '空'}")
    print("\n请检查 Grasshopper 画布上 Number Slider 的值是否为 0.75")

if __name__ == '__main__':
    main()