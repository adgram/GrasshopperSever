"""
测试提取 InstanceGuid
"""

import json

# 模拟从响应中获取的 Value 字符串
value = '组件添加成功{"ComponentGuid":"57da07bd-ecab-415d-9d86-af36d7073abc","InstanceGuid":"32aaf2e9-3a8e-4138-94e3-43243a8212b4","ComponentName":"Number Slider","Position":{"IsEmpty":false,"X":500,"Y":100},"State":"","Input":"","Output":""}'

print("原始 Value:")
print(value)
print()

# 方法1：直接解析（会失败）
print("方法1: 直接解析整个字符串")
try:
    component_info = json.loads(value)
    print(f"✓ 成功: {component_info}")
except json.JSONDecodeError as e:
    print(f"✗ 失败: {e}")
print()

# 方法2：找到第一个 '{' 的位置
print("方法2: 找到第一个 '{' 的位置，然后解析")
json_start = value.find('{')
print(f"'{{' 的位置: {json_start}")

if json_start != -1:
    json_str = value[json_start:]
    print(f"截取的 JSON 字符串: {json_str}")

    try:
        component_info = json.loads(json_str)
        instance_guid = component_info.get('InstanceGuid')
        print(f"✓ 解析成功")
        print(f"  InstanceGuid: {instance_guid}")
        print(f"  ComponentName: {component_info.get('ComponentName')}")
    except json.JSONDecodeError as e:
        print(f"✗ 失败: {e}")