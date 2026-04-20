"""
ADDCOMPONENTBYGUID 命令测试脚本
测试端口：9653
测试日期：2026-04-15
"""
from ghclient import GHClient
import time

PORT = 9653

def get_test_components():
    """获取测试用的组件 GUID（直接指定）"""
    # 指定的测试组件 GUID
    test_components = [
        {
            'name': 'Test Component 1',
            'guid': '2e3ab970-8545-46bb-836c-1c11e5610bce'
        },
        {
            'name': 'Test Component 2',
            'guid': '57da07bd-ecab-415d-9d86-af36d7073abc'
        },
        {
            'name': 'Test Component 3',
            'guid': 'bc3e379e-7206-4e7b-b63a-ff61f4b38a3e'
        },
        {
            'name': 'Test Component 4',
            'guid': '93b8e93d-f932-402c-b435-84be04d87666'
        },
        {
            'name': 'Test Component 5',
            'guid': 'A9A8EBD2-FFF5-4c44-A8F5-739736D129BA'
        }
    ]

    guids = {}
    for comp in test_components:
        guids[comp['name']] = {
            'guid': comp['guid']
        }
        print(f"✔️ {comp['name']}: {comp['guid']}")

    return guids

def test_add_by_guid(client, component_guid, component_name, x, y, test_number):
    """测试通过 GUID 添加组件"""
    print(f"\n{'='*70}")
    print(f"测试 #{test_number}: {component_name}")
    print(f"GUID: {component_guid}")
    print(f"位置：({x}, {y})")
    print(f"{'='*70}")

    try:
        # 发送 ADDCOMPONENTBYGUID 命令
        responses = client.send_command(
            name="Design",
            info="ADDCOMPONENTBYGUID 测试",
            value={
                "Command": "AddComponentByGuid",
                "ComponentGuid": component_guid,
                "X": x,
                "Y": y
            }
        )

        if responses:
            print(f"\n📨收到 {len(responses)} 条响应")
            for i, resp in enumerate(responses, 1):
                name = resp.get('Name', 'N/A')
                value = resp.get('Value', 'N/A')
                print(f"  [{i}] Name: {name}")
                print(f"      Value: {value}")

            return {
                'component': component_name,
                'guid': component_guid,
                'success': True,
                'responses': responses
            }
        else:
            print(f"\n❌未收到任何响应（超时）")
            return {
                'component': component_name,
                'guid': component_guid,
                'success': False,
                'responses': []
            }

    except Exception as e:
        print(f"\n❌错误：{e}")
        return {
            'component': component_name,
            'guid': component_guid,
            'success': False,
            'error': str(e)
        }

def main():
    """主测试函数"""
    print("="*70)
    print("ADDCOMPONENTBYGUID 命令测试")
    print("="*70)

    # 步骤 1: 获取测试组件 GUID
    print("\n步骤 1: 获取测试组件 GUID")
    print("-"*70)
    guids = get_test_components()

    if not guids:
        print("\n未找到任何组件 GUID，测试终止")
        return

    # 步骤 2: 测试每个组件
    print("\n\n步骤 2: 测试 ADDCOMPONENTBYGUID")
    print("="*70)

    results = []
    test_number = 1

    for component_name, info in guids.items():
        # 每次测试重新连接
        print(f"\n\n{'#'*70}")
        print(f"# 开始测试 #{test_number}: {component_name}")
        print(f"{'#'*70}")

        try:
            with GHClient(port = PORT) as client:
                print(f"✔️ 已连接到端口:{PORT}")
                
                time.sleep(0.5)

                # 测试组件添加
                result = test_add_by_guid(
                    client,
                    info['guid'],
                    component_name,
                    100 + (test_number - 1) * 100,
                    100,
                    test_number
                )
                results.append(result)
                test_number += 1

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
    print(f"{'#':<5} {'组件名称':<20} {'GUID':<40} {'状态':<10}")
    print(f"{'-'*70}")
    for i, result in enumerate(results, 1):
        status = '✅成功' if result['success'] else '❌失败'
        guid_short = result['guid'][:36]
        print(f"{i:<5} {result['component']:<20} {guid_short:<40} {status:<10}")

    print(f"\n{'='*70}")
    print(f"总计：{len(results)} 个测试 "
          f"{sum(1 for r in results if r['success'])} 成功，"
          f"{sum(1 for r in results if not r['success'])} 失败")
    print(f"{'='*70}")

if __name__ == '__main__':
    main()
