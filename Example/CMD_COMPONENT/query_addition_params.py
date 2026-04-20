"""
查询 Addition 组件的输入输出参数名
端口：9653
"""

import json
import sqlite3

# 数据库路径
DB_PATH = r"C:\Users\SZ\AppData\Roaming\Grasshopper\Libraries\GHserver\ComponentsInfo.db"

def query_database():
    """从数据库查询 Addition 组件信息"""
    print("="*60)
    print("从数据库查询 Addition 组件信息")
    print("="*60)
    
    try:
        conn = sqlite3.connect(DB_PATH)
        cursor = conn.cursor()
        
        # 查询 Addition 组件
        cursor.execute("""
            SELECT ComponentGuid, ComponentName, NickName, Description, Prototype
            FROM ALLCOMPS 
            WHERE ComponentName = 'Addition' OR NickName = 'Addition'
        """)
        
        results = cursor.fetchall()
        for row in results:
            print(f"\nComponentGuid: {row[0]}")
            print(f"ComponentName: {row[1]}")
            print(f"NickName: {row[2]}")
            print(f"Description: {row[3]}")
            print(f"Prototype: {row[4]}")
            
            # 解析 Prototype JSON
            if row[4]:
                try:
                    prototype = json.loads(row[4])
                    print(f"\n输入参数:")
                    for inp in prototype.get('Input', []):
                        print(f"  - {inp.get('Name', 'Unknown')} ({inp.get('Type', 'Unknown')})")
                    print(f"\n输出参数:")
                    for out in prototype.get('Output', []):
                        print(f"  - {out.get('Name', 'Unknown')} ({out.get('Type', 'Unknown')})")
                except:
                    pass
        
        conn.close()
    except Exception as e:
        print(f"[数据库查询错误] {e}")

def main():
    query_database()

if __name__ == '__main__':
    main()