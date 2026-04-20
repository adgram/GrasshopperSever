from ghclient import GHClient

if __name__ == "__main__":
    print("C# Script测试")
    print("=" * 50)
    data = {
        "name": "ScriptEditor",
        "info": "测试C#代码执行",
        "value": {
            "OUTPUT": "// Grasshopper Script Instance\n#region Usings\nusing System;\nusing System.Linq;\nusing System.Collections;\nusing System.Collections.Generic;\nusing System.Drawing;\n\nusing Rhino;\nusing Rhino.Geometry;\n\nusing Grasshopper;\nusing Grasshopper.Kernel;\nusing Grasshopper.Kernel.Data;\nusing Grasshopper.Kernel.Types;\n#endregion\n\npublic class Script_Instance : GH_ScriptInstance\n{\n    #region Notes\n    /* \n      Members:\n        RhinoDoc RhinoDocument\n        GH_Document GrasshopperDocument\n        IGH_Component Component\n        int Iteration\n\n      Methods (Virtual & overridable):\n        Print(string text)\n        Print(string format, params object[] args)\n        Reflect(object obj)\n        Reflect(object obj, string method_name)\n    */\n    #endregion\n\n    private void RunScript(object x, object y, ref object a)\n    {\n        // 创建一个点\n        Rhino.Geometry.Point3d point = new Rhino.Geometry.Point3d(10.0, 20.0, 30.0);\n        \n        // 返回点\n        a = point;\n    }\n}"
        }
    }
    with GHClient(port=6655) as client:
        responses = client.send_command(**data)
        print(responses)
    print("\n" + "=" * 50)
    print("测试完成!")