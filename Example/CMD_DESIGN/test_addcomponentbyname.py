"""
ADDCOMPONENTBYNAME 命令增强测试脚本 - 逐个测试，便于调试
测试端口：9653
测试日期：2026-04-15
"""

from ghclient import GHClient
import time

PORT = 9653

def test_single_component(client, component_name, x, y, usernick, test_number):
    """测试单个组件，返回详细信息"""
    print(f"\n{'='*70}")
    print(f"测试 #{test_number}: {component_name} 在位置 ({x}, {y})")
    print(f"{'='*70}")
    
    try:
        # 发送命令
        responses = client.send_command(
            name="Design",
            info="ADDCOMPONENTBYNAME 测试",
            value={
                "Command": "AddComponentByName",
                "ComponentName": component_name,
                "X": x,
                "Y": y,
                "USERNICK": usernick
            }
        )
        
        if responses:
            print(f"\n📨收到 {len(responses)} 条响应")
            return {
                'component': component_name,
                'success': True,
                'responses': responses
            }
        else:
            print(f"\n❌未收到任何响应（超时）")
            return {
                'component': component_name,
                'success': False,
                'responses': []
            }
            
    except Exception as e:
        print(f"\n❌错误：{e}")
        return {
            'component': component_name,
            'success': False,
            'error': str(e)
        }

def main():
    """主测试函数"""
    print("="*70)
    print("ADDCOMPONENTBYNAME 命令增强测试")
    print("每个组件独立测试，避免相互影响")
    print("="*70)
    
    # 测试用例 - 包含多种可能的名称格式
    test_cases = [
        ('Panel', 100, 100, "TestComp1"),
        ('Number Slider', 200, 100, "TestComp2")
    ]
    
    results = []
    
    for i, (component_name, x, y, usernick) in enumerate(test_cases, 1):
        # 每次测试重新连接，避免状态污染
        print(f"\n\n{'#'*70}")
        print(f"# 开始测试 #{i}: {component_name}")
        print(f"{'#'*70}")
        
        try:
            with GHClient(port = PORT) as client:
                print(f"✔️ 已连接到端口:{PORT}")
                
                # 等待一下确保连接稳定
                time.sleep(0.5)
                
                # 测试组件添加
                result = test_single_component(client, component_name, x, y, usernick, i)
                results.append(result)
                
        except ConnectionRefusedError:
            print(f"\n❌无法连接到端口:{PORT}")
            break
        except Exception as e:
            print(f"\n❌错误：{e}")
        finally:
            time.sleep(1)
    
    # 打印测试结果汇总
    print(f"\n\n{'='*70}")
    print("测试结果汇总")
    print(f"{'='*70}")
    print(f"{'#':<5} {'组件名称':<20} {'状态':<10} {'说明'}")
    print(f"{'-'*70}")
    for i, result in enumerate(results, 1):
        status = '✅成功' if result['success'] else '❌失败'
        component = result['component']
        note = ''
        if not result['success']:
            note = '无响应'
        print(f"{i:<5} {component:<20} {status:<10} {note}")
    
    print(f"\n{'='*70}")
    print(f"总计：{len(results)} 个测试 "
          f"{sum(1 for r in results if r['success'])} 成功，"
          f"{sum(1 for r in results if not r['success'])} 失败")
    print(f"{'='*70}")

if __name__ == '__main__':
    main()
