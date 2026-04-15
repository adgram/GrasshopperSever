"""
查找原生 Grasshopper Panel 组件的正确 GUID 和名称
"""

import sqlite3

db_path = r"C:\Users\[用户名]\AppData\Roaming\Grasshopper\Libraries\GrasshopperSever-net7.0-windows-v20260414\ComponentsInfo.db"
conn = sqlite3.connect(db_path)
cursor = conn.cursor()

# 查找所有包含 Panel 的组件
print("=== 所有包含 Panel 的组件 ===")
cursor.execute("""
    SELECT ComponentGuid, ComponentName, NickName, Category, SubCategory
    FROM ALLCOMPS
    WHERE ComponentName LIKE '%Panel%' OR NickName LIKE '%Panel%'
""")
for row in cursor.fetchall():
    print(f"GUID: {row[0]}")
    print(f"  Name: {row[1]}")
    print(f"  NickName: {row[2]}")
    print(f"  Category: {row[3]}/{row[4]}")
    print()

# 查找 Params/Input 分类下的组件
print("\n=== Params/Input 分类下的组件 ===")
cursor.execute("""
    SELECT ComponentGuid, ComponentName, NickName
    FROM ALLCOMPS
    WHERE Category = 'Params' AND SubCategory = 'Input'
    ORDER BY ComponentName
""")
for row in cursor.fetchall():
    print(f"Name: {row[1]}, NickName: {row[2]}, GUID: {row[0]}")

conn.close()
