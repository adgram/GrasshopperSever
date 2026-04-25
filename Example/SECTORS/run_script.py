
from ghclient import GHClient

script1 = """
using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using System.Text.Json;
using GrasshopperSever.Utils;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(ref object ljson)
    {
        ljson = new Ljson("objs", "all object", JsonSerializer.SerializeToElement(GetAllGHObjects()));
    }
    
    private static Dictionary<string, string> GetAllGHObjects()
    {
        Dictionary<string, string> result = new();
        
        // 获取当前 Grasshopper 文档
        GH_Document doc = Instances.ActiveCanvas?.Document;
        if (doc == null)
            return result;  // 无活动文档

        // 遍历所有对象（包括组件和参数）
        foreach (IGH_DocumentObject obj in doc.Objects)
        {
            // 跳过隐藏或已删除的对象（根据需求决定是否包含）
            if (obj == null) continue;
            // 获取实例 GUID 和 名称
            result[obj.InstanceGuid.ToString()] = obj.Name;

            // 如果对象是组件，遍历其输入参数和输出参数
            if (obj is IGH_Component comp)
            {
                // 输入参数
                foreach (IGH_Param input in comp.Params.Input)
                {
                    result[input.InstanceGuid.ToString()] = input.Name;
                }
                // 输出参数
                foreach (IGH_Param output in comp.Params.Output)
                {
                    result[output.InstanceGuid.ToString()] = output.Name;
                }
            }
        }
        
        return result;
    }
}
"""

def get_all_components(port):
    with GHClient(port=port) as client:
        responses = client.send_command(
            "RunScript",
            "查询文档所有对象",
            {"OUTPUT": script1}
        )
        print(responses)


if __name__ == '__main__':
    get_all_components(port=5695)

