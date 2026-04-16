"""
ADDPARAMWITHVALUE 命令测试脚本 - 测试添加各种参数组件并设置值
测试端口: 9653
测试日期: 2026-04-16
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
        'Info': 'ADDPARAMWITHVALUE测试',
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

def test_add_param(client, param_name, x, y, path=None, value=None, test_number=1):
    """测试添加参数组件并设置值"""
    print(f"\n{'='*70}")
    print(f"测试 #{test_number}: {param_name} 在位置 ({x}, {y})")
    print(f"{'='*70}")
    if path:
        print(f"Path: {path}")
    if value:
        print(f"Value: {value}")
    
    try:
        # 构建命令参数
        command_params = {
            'Command': 'AddParamWithValue',
            'ParamName': param_name,
            'X': x,
            'Y': y
        }
        
        if path is not None:
            command_params['Path'] = path
        
        if value is not None:
            command_params['Value'] = value
        
        # 发送命令
        send_command(client, 'Design', command_params)
        
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
                'param': param_name,
                'success': True,
                'responses': responses
            }
        else:
            print(f"\n✗ 未收到任何响应（超时）")
            return {
                'param': param_name,
                'success': False,
                'responses': []
            }
            
    except Exception as e:
        print(f"\n✗ 错误: {e}")
        import traceback
        traceback.print_exc()
        return {
            'param': param_name,
            'success': False,
            'error': str(e)
        }

def main():
    """主测试函数"""
    print("="*70)
    print("ADDPARAMWITHVALUE 命令测试")
    print("测试添加各种参数组件并设置值")
    print("="*70)
    
    # 测试用例
    test_cases = [
        # 测试1: 简单数字参数
        {
            'param_name': 'number',
            'x': 100,
            'y': 100,
            'value': '42.5',
            'description': '简单的数字参数'
        },
        
        # 测试2: 数字滑块
        {
            'param_name': 'slider',
            'x': 100,
            'y': 150,
            'value': '0.0 < 0.5 < 1.0',
            'description': '数字滑块，设置范围'
        },
        
        # 测试3: 文本参数
        {
            'param_name': 'text',
            'x': 100,
            'y': 200,
            'value': 'Hello Grasshopper',
            'description': '文本参数'
        },
        
        # 测试4: 布尔参数
        {
            'param_name': 'bool',
            'x': 100,
            'y': 250,
            'value': 'true',
            'description': '布尔参数'
        },
        
        # 测试5: 布尔开关（True）
        {
            'param_name': 'true',
            'x': 100,
            'y': 300,
            'description': '布尔开关（True）'
        },
        
        # 测试6: 带数据路径的数字参数
        {
            'param_name': 'number',
            'x': 100,
            'y': 350,
            'path': '{0;1;2}',
            'value': '["1.0", "2.0", "3.0", "4.0", "5.0"]',
            'description': '带数据路径的数字参数（列表值）'
        },
        
        # 测试7: 整数参数
        {
            'param_name': 'int',
            'x': 100,
            'y': 400,
            'value': '42',
            'description': '整数参数'
        },
        
        # 测试8: 面板
        {
            'param_name': 'panel',
            'x': 100,
            'y': 450,
            'value': 'Panel text\nLine 2\nLine 3',
            'description': '面板，支持多行文本'
        },
        
        # 测试9: 点参数
        {
            'param_name': 'point',
            'x': 300,
            'y': 100,
            'value': '{10.0, 20.0, 30.0}',
            'description': '点参数'
        },
        
        # 测试10: 向量参数
        {
            'param_name': 'vector',
            'x': 300,
            'y': 150,
            'value': '{1.0, 2.0, 3.0}',
            'description': '向量参数'
        },
        
        # 测试11: 颜色参数
        {
            'param_name': 'color',
            'x': 300,
            'y': 200,
            'value': 'White',
            'description': '颜色参数'
        },
        
        # 测试12: 切换按钮
        {
            'param_name': 'toggle',
            'x': 300,
            'y': 250,
            'value': 'false',
            'description': '切换按钮'
        },
        
        # 测试13: 带数据路径的文本参数
        {
            'param_name': 'text',
            'x': 300,
            'y': 300,
            'path': '{0}',
            'value': '["Item1", "Item2", "Item3"]',
            'description': '带数据路径的文本参数（列表值）'
        },
    ]
    
    results = []
    
    for i, test_case in enumerate(test_cases, 1):
        # 每次测试重新连接，避免状态污染
        print(f"\n\n{'#'*70}")
        print(f"# 开始测试 #{i}: {test_case['description']}")
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
            
            # 测试参数添加
            result = test_add_param(
                client,
                test_case['param_name'],
                test_case['x'],
                test_case['y'],
                path=test_case.get('path'),
                value=test_case.get('value'),
                test_number=i
            )
            result['description'] = test_case['description']
            results.append(result)
            
        except ConnectionRefusedError:
            print(f"\n✗ 无法连接到 {HOST}:{PORT}")
            break
        except Exception as e:
            print(f"\n✗ 错误: {e}")
        finally:
            client.close()
            time.sleep(0.5)  # 等待连接完全关闭
    
    # 打印测试结果汇总
    print(f"\n\n{'='*70}")
    print("测试结果汇总")
    print(f"{'='*70}")
    print(f"{'#':<5} {'参数类型':<15} {'描述':<30} {'状态':<10}")
    print(f"{'-'*70}")
    for i, result in enumerate(results, 1):
        status = '✓ 成功' if result['success'] else '✗ 失败'
        param = result['param']
        desc = result.get('description', 'N/A')
        print(f"{i:<5} {param:<15} {desc:<30} {status:<10}")
    
    print(f"\n{'='*70}")
    print(f"总计: {len(results)} 个测试, "
          f"{sum(1 for r in results if r['success'])} 成功, "
          f"{sum(1 for r in results if not r['success'])} 失败")
    print(f"{'='*70}")

if __name__ == '__main__':
    main()