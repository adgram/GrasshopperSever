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


def test_save_document():
    """测试保存文档"""
    print("=" * 50)
    print("测试1: 保存当前文档")
    print("=" * 50)

    return send_command(6879, "DOCUMENT", "SAVEDOCUMENT", {})

def test_save_document_with_path():
    """测试保存文档到指定路径"""
    print("\n" + "=" * 50)
    print("测试2: 保存文档到指定路径")
    print("=" * 50)

    save_path = r"C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\test\test_save.gh"

    return send_command(6879, "DOCUMENT", "SAVEDOCUMENT", {
        "FilePath": save_path
    })

def test_load_document():
    """测试加载文档"""
    print("\n" + "=" * 50)
    print("测试3: 加载文档")
    print("=" * 50)

    # 先检查是否有可用的gh文件
    import os
    test_dir = r"C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GHserver\test"
    gh_files = [f for f in os.listdir(test_dir) if f.endswith('.gh')]

    if gh_files:
        load_path = os.path.join(test_dir, gh_files[0])
        print(f"加载文件: {load_path}")
    else:
        print("测试目录中没有.gh文件，跳过加载测试")
        return []

    return send_command(6879, "DOCUMENT", "LOADDOCUMENT", {
        "FilePath": load_path
    })


def test_database_path():
    """测试获取数据库路径"""
    print("\n" + "=" * 50)
    print("测试4: 获取数据库路径")
    print("=" * 50)

    return send_command(6879, "DOCUMENT", "DATABASEPATH", {})


if __name__ == "__main__":
    print("Grasshopper文档API测试")
    print("=" * 50)

    # 测试获取数据库路径
    test_database_path()

    # 测试保存文档
    test_save_document()

    # 测试保存到指定路径
    test_save_document_with_path()

    # 测试加载文档
    test_load_document()

    print("\n" + "=" * 50)
    print("测试完成!")
    print("=" * 50)
