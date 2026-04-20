from ghclient import GHClient
import json

def send_command(port, ljson_type, command_name, params):
    """发送命令到GrasshopperSever"""
    data = {
        "name": ljson_type,
        "info": f"执行{command_name}命令",
        "value": {
            "Command": command_name,
            **params
        }
    }
    responses = []
    with GHClient(port = port) as client:
        responses = client.send_command(**data)
        print(responses)
    return responses

def test_select_objects():
    """测试SELECTOBJECTS命令"""
    print("=" * 60)
    print("测试3: SELECTOBJECTS命令（端口6655）")
    print("=" * 60)

    # 步骤1: 创建一些对象
    print("步骤1: 创建多个对象")
    scripts = [
        "_-Point 0,0,0",
        "_-Point 5,5,0", 
        "_-Point 10,10,0"
    ]
    
    created_guids = []
    for script in scripts:
        print(f"执行脚本: {script}")
        results = send_command(6655, "RHINO", "RHINOSCRIPT", {
            "Script": script
        })
        
        # 获取最后创建的对象
        get_results = send_command(6655, "RHINO", "GETLASTCREATEDOBJECTS", {})
        for result in get_results:
            if result.get('Name') == 'GetLastCreatedObjects':
                for key, value in result.get('Value', {}).items():
                    if key.startswith('Object_'):
                        created_guids.append(value.get('Id'))
                        print(f"  创建对象ID: {value.get('Id')}")
                        break
    
    print(f"\n总共创建了 {len(created_guids)} 个对象")
    
    # 步骤2: 测试选择这些对象
    print(f"\n步骤2: 选择 {len(created_guids)} 个对象")
    if created_guids:
        objects_str = ",".join(created_guids)
        print(f"选择对象ID: {objects_str}")
        
        results = send_command(6655, "RHINO", "SELECTOBJECTS", {
            "Objects": objects_str
        })
        
        print(results)
    return results

def test_select_invalid_objects():
    """测试选择无效对象ID"""
    print("\n" + "=" * 60)
    print("测试选择无效对象ID")
    print("=" * 60)

    # 测试无效的GUID
    invalid_guids = "00000000-0000-0000-0000-000000000000,invalid-guid,12345"
    print(f"尝试选择无效对象ID: {invalid_guids}")

    results = send_command(6655, "RHINO", "SELECTOBJECTS", {
        "Objects": invalid_guids
    })

    print(results)
    return results

def test_get_and_select_last_objects():
    """测试GETANDSELECTLASTOBJECTS命令"""
    print("\n" + "=" * 60)
    print("测试4: GETANDSELECTLASTOBJECTS命令（端口6655）")
    print("=" * 60)

    # 步骤1: 创建一个对象
    print("步骤1: 创建一个圆对象")
    script = "_-Circle 0,0,0 5"
    print(f"执行脚本: {script}")

    results = send_command(6655, "RHINO", "RHINOSCRIPT", {
        "Script": script
    })

    print(results)
    
    # 步骤2: 执行GETANDSELECTLASTOBJECTS命令
    print("\n步骤2: 执行GETANDSELECTLASTOBJECTS命令")
    results = send_command(6655, "RHINO", "GETANDSELECTLASTOBJECTS", {})

    print(results)
    return results

if __name__ == "__main__":
    print("SELECTOBJECTS 和 GETANDSELECTLASTOBJECTS 命令测试")
    print("=" * 60)
    # 测试SELECTOBJECTS命令
    test_select_objects()

    # 测试选择无效对象
    test_select_invalid_objects()

    # 测试GETANDSELECTLASTOBJECTS命令
    test_get_and_select_last_objects()

    print("\n" + "=" * 60)
    print("测试完成!")
    print("=" * 60)