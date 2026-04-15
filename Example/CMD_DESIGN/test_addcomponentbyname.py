"""
ADDCOMPONENTBYNAME 命令增强测试脚本 - 逐个测试，便于调试
测试端口: 9653
测试日期: 2026-04-15
"""

import socket
import json
from datetime import datetime
import time

HOST = '127.0.0.1'
PORT = 9653

def send_command(client, name, value):
    """发送命令到 GrasshopperSever"""
    data = {
        'Name': name,
        'Info': 'ADDCOMPONENTBYNAME测试',
        'Time': datetime.now().isoformat(),
        'Value': value
    }
    message = json.dumps(data, ensure_ascii=False)
    print(f"\n发送: {message}")
    client.sendall((message + '\n').encode('utf-8'))

def receive_all(client, timeout=5):
    """接收所有响应"""
    client.settimeout(timeout)
    total_response = b''
    try:
        while True:
            chunk = client.recv(8192)
            if not chunk:
                break
            total_response += chunk
            # 如果收到完整消息，尝试提前结束
            if b'"Value"' in chunk and b'}' in chunk:
                time.sleep(0.2)  # 等待可能的后续消息
                break
    except socket.timeout:
        pass

    if total_response:
        response = total_response.decode('utf-8-sig')
        messages = [msg for msg in response.split('\ufeff') if msg.strip()]
        return [json.loads(msg.strip()) for msg in messages]
    return []

def test_single_component(client, component_name, x, y, test_number):
    """测试单个组件，返回详细信息"""
    print(f"\n{'='*70}")
    print(f"测试 #{test_number}: {component_name} 在位置 ({x}, {y})")
    print(f"{'='*70}")
    
    try:
        # 发送命令
        send_command(client, 'Design', {
            'Command': 'AddComponentByName',
            'ComponentName': component_name,
            'X': x,
            'Y': y
        })
        
        # 接收响应
        responses = receive_all(client)
        
        if responses:
            print(f"\n✓ 收到 {len(responses)} 条响应:")
            for i, resp in enumerate(responses, 1):
                name = resp.get('Name', 'N/A')
                value = resp.get('Value', 'N/A')
                print(f"  [{i}] Name: {name}")
                print(f"      Value: {value}")
                
                # 检查是否包含组件信息
                if isinstance(value, str) and 'InstanceGuid' in value:
                    import ast
                    try:
                        value_dict = ast.literal_eval(value.split('组件添加成功')[-1].strip())
                        print(f"\n  📋 组件详情:")
                        print(f"     ComponentGuid: {value_dict.get('ComponentGuid', 'N/A')}")
                        print(f"     InstanceGuid: {value_dict.get('InstanceGuid', 'N/A')}")
                        print(f"     ComponentName: {value_dict.get('ComponentName', 'N/A')}")
                        print(f"     Position: ({value_dict.get('Position', {}).get('X', 'N/A')}, {value_dict.get('Position', {}).get('Y', 'N/A')})")
                    except:
                        pass
            
            return {
                'component': component_name,
                'success': True,
                'responses': responses
            }
        else:
            print(f"\n✗ 未收到任何响应（超时）")
            return {
                'component': component_name,
                'success': False,
                'responses': []
            }
            
    except Exception as e:
        print(f"\n✗ 错误: {e}")
        return {
            'component': component_name,
            'success': False,
            'error': str(e)
        }

def main():
    """主测试函数"""
    print("="*70)
    print("ADDCOMPONENTBYNAME 命令增强测试")
    print("每个组件独立测试，避免互相影响")
    print("="*70)
    
    # 测试用例 - 包含多种可能的名称格式
    test_cases = [
        ('Panel', 100, 100),
        ('Number Slider', 200, 100)
    ]
    
    results = []
    
    for i, (component_name, x, y) in enumerate(test_cases, 1):
        # 每次测试重新连接，避免状态污染
        print(f"\n\n{'#'*70}")
        print(f"# 开始测试 #{i}: {component_name}")
        print(f"{'#'*70}")
        
        client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        try:
            client.connect((HOST, PORT))
            print(f"✓ 已连接到 {HOST}:{PORT}")
            
            # 接收连接响应
            init_responses = receive_all(client, timeout=2)
            if init_responses:
                print(f"连接响应: {init_responses}")
            
            # 等待一下确保连接稳定
            time.sleep(0.5)
            
            # 测试组件添加
            result = test_single_component(client, component_name, x, y, i)
            results.append(result)
            
        except ConnectionRefusedError:
            print(f"\n✗ 无法连接到 {HOST}:{PORT}")
            break
        except Exception as e:
            print(f"\n✗ 错误: {e}")
        finally:
            client.close()
            time.sleep(1)  # 等待连接完全关闭
    
    # 打印测试结果汇总
    print(f"\n\n{'='*70}")
    print("测试结果汇总")
    print(f"{'='*70}")
    print(f"{'#':<5} {'组件名称':<20} {'状态':<10} {'说明'}")
    print(f"{'-'*70}")
    for i, result in enumerate(results, 1):
        status = '✓ 成功' if result['success'] else '✗ 失败'
        component = result['component']
        note = ''
        if not result['success']:
            note = '无响应'
        print(f"{i:<5} {component:<20} {status:<10} {note}")
    
    print(f"\n{'='*70}")
    print(f"总计: {len(results)} 个测试, "
          f"{sum(1 for r in results if r['success'])} 成功, "
          f"{sum(1 for r in results if not r['success'])} 失败")
    print(f"{'='*70}")

if __name__ == '__main__':
    main()
