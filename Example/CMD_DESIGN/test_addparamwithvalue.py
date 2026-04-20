"""
ADDPARAMWITHVALUE 命令测试脚本 - 测试添加各种参数组件并设置值
测试端口：9653
测试日期：2026-04-16
"""

from ghclient import GHClient
import time


def test_add_param(client: GHClient, param_name, x, y, path=None, value=None):
    """测试添加参数组件并设置值"""

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
    responses = client.send_command(
        name="Design",
        info="ADDPARAMWITHVALUE 测试",
        value=command_params
    )
    
    return {
        'param': param_name,
        'success': len(responses) == 2,
        'responses': responses
    }





def main():
    """主测试函数"""
    print("="*70)
    print("ADDPARAMWITHVALUE 命令测试")
    print("测试添加各种参数组件并设置值")
    print("="*70)
    
    # 测试用例
    test_cases = [
        # 测试 1: 简单数字参数
        {
            'param_name': 'number',
            'x': 100,
            'y': 100,
            'value': '42.5',
            'description': '简单的数字参数'
        },
        
        # 测试 2: 数字滑块
        {
            'param_name': 'slider',
            'x': 100,
            'y': 150,
            'value': '0.0 < 0.5 < 1.0',
            'description': '数字滑块，设置范围'
        },
        
        # 测试 3: 文本参数
        {
            'param_name': 'text',
            'x': 100,
            'y': 200,
            'value': 'Hello Grasshopper',
            'description': '文本参数'
        },
        
        # 测试 4: 布尔参数
        {
            'param_name': 'bool',
            'x': 100,
            'y': 250,
            'value': 'true',
            'description': '布尔参数'
        },
        
        # 测试 5: 布尔开关（True）
        {
            'param_name': 'true',
            'x': 100,
            'y': 300,
            'description': '布尔开关（True）'
        },
        
        # 测试 6: 带数据路径的数字参数
        {
            'param_name': 'number',
            'x': 100,
            'y': 350,
            'path': '{0;1;2}',
            'value': '["1.0", "2.0", "3.0", "4.0", "5.0"]',
            'description': '带数据路径的数字参数（列表值）'
        },
        
        # 测试 7: 整数参数
        {
            'param_name': 'int',
            'x': 100,
            'y': 400,
            'value': '42',
            'description': '整数参数'
        },
        
        # 测试 8: 面板
        {
            'param_name': 'panel',
            'x': 100,
            'y': 450,
            'value': 'Panel text\nLine 2\nLine 3',
            'description': '面板，支持多行文本'
        },
        
        # 测试 9: 点参数
        {
            'param_name': 'point',
            'x': 300,
            'y': 100,
            'value': '{10.0, 20.0, 30.0}',
            'description': '点参数'
        },
        
        # 测试 10: 向量参数
        {
            'param_name': 'vector',
            'x': 300,
            'y': 150,
            'value': '{1.0, 2.0, 3.0}',
            'description': '向量参数'
        },
        
        # 测试 11: 颜色参数
        {
            'param_name': 'color',
            'x': 300,
            'y': 200,
            'value': 'White',
            'description': '颜色参数'
        },
        
        # 测试 12: 切换按钮
        {
            'param_name': 'toggle',
            'x': 300,
            'y': 250,
            'value': 'false',
            'description': '切换按钮'
        },
        
        # 测试 13: 带数据路径的文本参数
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
        PORT = 9653  # 每次测试使用同一端口，确保服务器已重启
        try:
            with GHClient(port = PORT) as client:
                print(f"✔️ 已连接到端口:{PORT}")
                
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
                )
                result['description'] = test_case['description']
                results.append(result)
                
        except ConnectionRefusedError:
            print(f"\n❌无法连接到端口:{PORT}")
            break
        except Exception as e:
            print(f"\n❌错误：{e}")
        finally:
            time.sleep(0.5)
    
    # 打印测试结果汇总
    print(f"\n\n{'='*70}")
    print("测试结果汇总")
    print(f"{'='*70}")
    print(f"{'#':<5} {'参数类型':<15} {'描述':<30} {'状态':<10}")
    print(f"{'-'*70}")
    for i, result in enumerate(results, 1):
        status = '✅成功' if result['success'] else '❌失败'
        param = result['param']
        desc = result.get('description', 'N/A')
        print(f"{i:<5} {param:<15} {desc:<30} {status:<10}")
    
    print(f"\n{'='*70}")
    print(f"总计：{len(results)} 个测试 "
          f"{sum(1 for r in results if r['success'])} 成功，"
          f"{sum(1 for r in results if not r['success'])} 失败")
    print(f"{'='*70}")

if __name__ == '__main__':
    main()
