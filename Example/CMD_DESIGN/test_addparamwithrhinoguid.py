"""
ADDPARAMWITHVALUE 命令测试脚本 - 测试添加各种参数组件并设置值
测试端口：9653
测试日期：2026-04-16
"""

from ghclient import GHClient
from test_addparamwithvalue import test_add_param



if __name__ == '__main__':
    
    # 测试用例
    test_case = {
        'param_name': 'curve',
        'x': 100,
        'y': 100,
        'path': '{0}',
        'value': '["9a5ae871-8d49-4398-a1a3-1a3152f39018"]',
        'description': '拾取Rhino中的曲线对象作为参数值'
    }
    

    with GHClient(port = 9653) as client:
        # 测试参数添加
        result = test_add_param(
            client,
            test_case['param_name'],
            test_case['x'],
            test_case['y'],
            test_case['path'],
            test_case['value']
        )
        result['description'] = test_case['description']
        print(f"\n\n{'#'*70}")
        print(f"# 测试结果: {result['description']}")
        print(f"成功: {result['success']}")
        print(f"响应详情: {result['responses']}")