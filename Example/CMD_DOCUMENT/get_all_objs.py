"""
获取所有对象
"""
from ghclient import GHClient

data1 = {
    'name': 'DOCUMENT',
    'info': '获取所有对象',
    'value': {'Command': 'GETALLOBJECTS'}
}
data2 = {
    'name': 'DOCUMENT',
    'info': '获取单个对象',
    'value': {'Command': 'GETOBJECT', 'guid': '7f589226-c022-4397-ac5a-7737f283bb42'}
}

with GHClient(port=6879) as client:
    responses1 = client.send_command(**data1)
    responses2 = client.send_command(**data2)
    print(responses1)
    print(responses2)


'''
[{'Name': 'OK', 'Info': '成功响应', 'Time': '2026-04-24T23:38:35.6097302+08:00', 'Value': '数据接收成功'}, {'Name': 'DocumentGraph', 'Info': '当前文档所有对象', 'Time': '2026-04-24T23:38:35.6441508+08:00', 'Value': {'DocumentId': '56c54005-bb2f-4c5c-86aa-e69dc2abbd16', 'TotalCount': 9, 'Graph': {'Heads': ['12e4069c-04b0-43f0-9374-217a1ae84f77', '08d583bb-90e4-4bc3-bda1-c2b05615e5a7', '19777798-0c1f-4dd4-a709-dbc538572700', '6003cd6b-b6a4-4c78-ad8f-57545173aba6', '56c347d8-722b-48d0-b3ff-e44076667a27', '02c24206-0073-412e-95cf-b093cf6eaafc'], 'Nodes': [{'Id': '12e4069c-04b0-43f0-9374-217a1ae84f77', 'Name': 'Boolean Toggle', 'Type': 'TopParam'}, {'Id': '08d583bb-90e4-4bc3-bda1-c2b05615e5a7', 'Name': 'Panel', 'Type': 'TopParam'}, {'Id': 'b8b606d4-82c8-42f3-8779-758aa491b0e8', 'Name': 'GHServer', 'Type': 'Component'}, {'Id': 'f65579ad-6330-471d-8b41-0222a2b6843f', 'Name': 'Panel', 'Type': 'TopParam'}, {'Id': '19777798-0c1f-4dd4-a709-dbc538572700', 'Name': 'Number Slider', 'Type': 'TopParam'}, {'Id': '6003cd6b-b6a4-4c78-ad8f-57545173aba6', 'Name': 'Number Slider', 'Type': 'TopParam'}, {'Id': '56c347d8-722b-48d0-b3ff-e44076667a27', 'Name': 'Number Slider', 'Type': 'TopParam'}, {'Id': '02c24206-0073-412e-95cf-b093cf6eaafc', 'Name': 'Addition', 'Type': 'Component'}, {'Id': '7f589226-c022-4397-ac5a-7737f283bb42', 'Name': 'Larger Than', 'Type': 'Component'}], 'Adjacency': {'12e4069c-04b0-43f0-9374-217a1ae84f77': {'Boolean Toggle': {'b8b606d4-82c8-42f3-8779-758aa491b0e8': 'Enabled'}}, '08d583bb-90e4-4bc3-bda1-c2b05615e5a7': {'Panel': {'b8b606d4-82c8-42f3-8779-758aa491b0e8': 'Port'}}, 'b8b606d4-82c8-42f3-8779-758aa491b0e8': {'Status': {'f65579ad-6330-471d-8b41-0222a2b6843f': 'Panel'}, 'OutPut': {}}, 'f65579ad-6330-471d-8b41-0222a2b6843f': {'Panel': {}}, '19777798-0c1f-4dd4-a709-dbc538572700': {'Number Slider': {}}, '6003cd6b-b6a4-4c78-ad8f-57545173aba6': {'Number Slider': {}}, '56c347d8-722b-48d0-b3ff-e44076667a27': {'Number Slider': {}}, '02c24206-0073-412e-95cf-b093cf6eaafc': {'Result': {'7f589226-c022-4397-ac5a-7737f283bb42': 'First Number'}}, '7f589226-c022-4397-ac5a-7737f283bb42': {'Larger than': {}, '… or Equal to': {}}}}}}]
[{'Name': 'OK', 'Info': '成功响应', 'Time': '2026-04-24T23:38:35.7112817+08:00', 'Value': '数据接收成功'}, {'Name': 'Component', 'Info': '查找的实例对象', 'Time': '2026-04-24T23:38:35.7417839+08:00', 'Value': {'ComponentGuid': '30d58600-1aab-42db-80a3-f1ea6c4269a0', 'InstanceGuid': '7f589226-c022-4397-ac5a-7737f283bb42', 'Name': 'Larger Than', 'NickName': 'Larger', 'Category': 'Maths', 'SubCategory': 'Operators', 'State': '', 'Inputs': [{'ParamGuid': '3e8ca6be-fda8-4aaf-b5c0-3c54c8bb7312', 'InstanceGuid': '03c1b275-f34e-49d8-95b2-07dced2da880', 'Name': 'First Number', 'NickName': 'A', 'Description': 'Number to test', 'TypeName': 'Number', 'Optional': False, 'Access': 'item', 'Mapping': 'None', 'Reverse': False, 'Simplify': False, 'Sources': '["7b3fb6cb-8e46-4653-a7fd-8e9d868e05b5"]', 'Recipients': '[]'}, {'ParamGuid': '3e8ca6be-fda8-4aaf-b5c0-3c54c8bb7312', 'InstanceGuid': '93686ddc-acab-4975-a10b-f44a060a171e', 'Name': 'Second Number', 'NickName': 'B', 'Description': 'Number to test against', 'TypeName': 'Number', 'Optional': False, 'Access': 'item', 'Mapping': 'None', 'Reverse': False, 'Simplify': False, 'Sources': '[]', 'Recipients': '[]'}], 'Outputs': [{'ParamGuid': 'cb95db89-6165-43b6-9c41-5702bc5bf137', 'InstanceGuid': '9d943cd4-1f63-48b8-94be-3061ea806e3d', 'Name': 'Larger than', 'NickName': '>', 'Description': 'True if A > B', 'TypeName': 'Boolean', 'Optional': False, 'Access': 'item', 'Mapping': 'None', 'Reverse': False, 'Simplify': False, 'Sources': '[]', 'Recipients': '[]'}, {'ParamGuid': 'cb95db89-6165-43b6-9c41-5702bc5bf137', 'InstanceGuid': 'ccd1a673-f69f-4c5f-9f9e-40de8f6fa071', 'Name': '… or Equal to', 'NickName': '>=', 'Description': 'True if A >= B', 'TypeName': 'Boolean', 'Optional': False, 'Access': 'item', 'Mapping': 'None', 'Reverse': False, 'Simplify': False, 'Sources': '[]', 'Recipients': '[]'}], 'Type': 'Component', 'Position': {'IsEmpty': False, 'X': 137, 'Y': 231}}}]
'''