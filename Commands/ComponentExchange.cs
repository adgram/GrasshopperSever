using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    public class ComponentExchange
    {
        /// <summary>
        /// 创建组件信息Ljson
        /// </summary>
        public static Ljson InstanceLjson(string componentGuid,
            string instanceGuid, string name, PointF position,
            string state, string input, string output)
        {
            var data = new Dictionary<string, JsonElement>
            {
                { "ComponentGuid", JsonSerializer.SerializeToElement(componentGuid) },
                { "InstanceGuid", JsonSerializer.SerializeToElement(instanceGuid) },
                { "ComponentName", JsonSerializer.SerializeToElement(name) },
                { "Position", JsonSerializer.SerializeToElement(position) },
                { "State", JsonSerializer.SerializeToElement(state) },
                { "Input", JsonSerializer.SerializeToElement(input) },
                { "Output", JsonSerializer.SerializeToElement(output) }
            };

            return new Ljson("Component", "组件信息", JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByLjson(Ljson ljson, PointF point)
        {
            var componentGuid = ljson.GetParameterString("ComponentGuid");
            return AddComponentByGuid(componentGuid, point);
        }

        /// <summary>
        /// 通过组件 GUID 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByGuid(string guid, PointF point)
        {
            Exception caughtException = null;
            IGH_DocumentObject dobj = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");
                    dobj = Instances.ComponentServer.EmitObject(new Guid(guid));

                    if (dobj != null)
                    {
                        // 确保有属性
                        if (dobj.Attributes == null)
                        {
                            dobj.CreateAttributes();
                        }

                        // 设置位置
                        dobj.Attributes.Pivot = point;
                        doc.AddObject(dobj, false);
                        doc.NewSolution(false);
                    }
                    else
                    {
                        caughtException = new InvalidOperationException($"Failed to create component with GUID: {guid}");
                    }
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            }));

            if (caughtException != null)
            {
                throw caughtException;
            }

            return RecordAddComponent(dobj, point);
        }

        public static Ljson AddComponent(IGH_DocumentObject dobj, PointF point)
        {
            if (dobj == null) throw new InvalidOperationException("Failed to create component");
            Exception caughtException = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");
                    if (dobj != null)
                    {
                        // 确保有属性
                        if (dobj.Attributes == null)
                        {
                            dobj.CreateAttributes();
                        }
                        // 设置位置
                        dobj.Attributes.Pivot = point;
                        doc.AddObject(dobj, false);
                        doc.NewSolution(false);
                    }
                    else
                    {
                        caughtException = new InvalidOperationException("Failed to create component with DocumentObjec");
                    }
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            }));

            if (caughtException != null)
            {
                throw caughtException;
            }

            return RecordAddComponent(dobj, point);
        }

        /// <summary>
        /// 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson RecordAddComponent(IGH_DocumentObject component, PointF point)
        {
            if (component == null) throw new InvalidOperationException("Failed to create component");

            // 记录添加组件操作到数据库
            ComponentExchangeDB.RecordAddComponent(
                componentGuid: component.ComponentGuid.ToString(),
                instanceGuid: component.InstanceGuid.ToString(),
                componentName: component.Name,
                x: point.X,
                y: point.Y,
                description: $"添加组件 {component.Name}"
            );

            string inputsJson = "";
            string outputsJson = "";
            if (component is IGH_Component cominst)
            {
                inputsJson = ParamExchange.SerializeParamDefinitions(cominst.Params.Input).ToString();
                outputsJson = ParamExchange.SerializeParamDefinitions(cominst.Params.Output).ToString();
            }else if(component is IGH_Param parins)
            {
                inputsJson = "不包含自定义值";
            }
            // 返回 InstanceLjson
            return InstanceLjson(
                componentGuid: component.ComponentGuid.ToString(),
                instanceGuid: component.InstanceGuid.ToString(),
                name: component.Name,
                position: point,
                state: "",
                input: inputsJson,
                output: outputsJson
            );
        }


        /// <summary>
        /// 通过组件名称添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByName(string name, PointF point)
        {
            string guid = ComponentInfo.FindComponentsGuidByName(name);
            if (guid != null) return AddComponentByGuid(guid, point);
            
            throw new InvalidOperationException($"Failed to find component with name: {name}");
        }

        /// <summary>
        /// 从 Grasshopper 文档中移除组件
        /// </summary>
        public static bool RemoveComponent(string guid)
        {
            bool success = false;
            Exception caughtException = null;
            string componentName = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");

                    // 查找组件
                    var component = doc.FindObject(new Guid(guid), false);

                    if (component != null)
                    {
                        componentName = component.Name;

                        // 移除组件
                        doc.RemoveObject(component, false);
                        doc.NewSolution(false);
                        success = true;
                    }
                    else
                    {
                        caughtException = new InvalidOperationException($"Failed to find component with instance GUID: {guid}");
                    }
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            }));

            if (caughtException != null)
            {
                throw caughtException;
            }

            // 记录删除组件操作到数据库
            if (success && !string.IsNullOrEmpty(componentName))
            {
                ComponentExchangeDB.RecordRemoveComponent(
                    instanceGuid: guid,
                    componentName: componentName,
                    description: $"删除组件 {componentName}"
                );
            }

            return success;
        }

        public static IGH_Param FindParam(GH_Document doc, string guid, string name, bool isIn)
        {
            if (!Guid.TryParse(guid, out Guid uid)){
                throw new ArgumentException("Invalid component ID format");
            }
            var p = doc.FindParameter(uid);
            if (p != null) return p;

            var c = doc.FindComponent(uid) ?? throw new ArgumentException($"Source or target {uid} not found");
            List<IGH_Param> ps = c.Params.Output;
            if (isIn) ps = c.Params.Input;
            
            foreach (var param in ps)
            {
                if (param.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return param;
                }
            }
            throw new ArgumentException($"Source or target {name} not found");
        }

        /// <summary>
        /// 连接两个组件的参数
        /// </summary>
        /// <param name="fromGuid">源组件的实例 GUID</param>
        /// <param name="fromParameter">源组件的输出参数名称</param>
        /// <param name="toGuid">目标组件的实例 GUID</param>
        /// <param name="toParameter">目标组件的输入参数名称</param>
        /// <returns>连接是否成功</returns>
        public static bool ConnectComponents(string fromGuid, string fromParameter, string toGuid, string toParameter)
        {
            if (string.IsNullOrEmpty(fromGuid) || string.IsNullOrEmpty(toGuid))
            {
                throw new ArgumentException("Source and target component information are required");
            }

            bool result = false;
            Exception exception = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");

                    var fromParam = FindParam(doc, fromGuid, fromParameter, false);
                    var toParam = FindParam(doc, toGuid, toParameter, true);

                    toParam.AddSource(fromParam);
                    doc.NewSolution(false);

                    result = true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }));

            if (exception != null)
            {
                throw exception;
            }

            // 记录连接组件操作到数据库
            if (result)
            {
                ComponentExchangeDB.RecordConnectComponents(
                    fromInstanceGuid: fromGuid,
                    fromParameter: fromParameter,
                    toInstanceGuid: toGuid,
                    toParameter: toParameter,
                    description: $"连接组件 {fromGuid}.{fromParameter} 到 {toGuid}.{toParameter}"
                );
            }

            return result;
        }

        /// <summary>
        /// 断开两个组件参数之间的连接
        /// </summary>
        /// <param name="fromGuid">源组件的实例 GUID</param>
        /// <param name="fromParameter">源组件的输出参数名称</param>
        /// <param name="toGuid">目标组件的实例 GUID</param>
        /// <param name="toParameter">目标组件的输入参数名称</param>
        /// <returns>断开连接是否成功</returns>
        public static bool DisconnectComponents(string fromGuid, string fromParameter, string toGuid, string toParameter)
        {
            if (string.IsNullOrEmpty(fromGuid) || string.IsNullOrEmpty(fromParameter) ||
                string.IsNullOrEmpty(toGuid) || string.IsNullOrEmpty(toParameter))
            {
                throw new ArgumentException("Source and target component information are required");
            }

            bool result = false;
            Exception exception = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");


                    var fromParam = FindParam(doc, fromGuid, fromParameter, false);
                    var toParam = FindParam(doc, toGuid, toParameter, true);

                    // 检查是否存在连接
                    if (!toParam.Sources.Contains(fromParam))
                    {
                        throw new InvalidOperationException("No connection exists between the specified parameters");
                    }

                    // 断开连接
                    toParam.RemoveSource(fromParam);
                    doc.NewSolution(false);

                    result = true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }));

            if (exception != null)
            {
                throw exception;
            }

            // 记录断开连接操作到数据库
            if (result)
            {
                ComponentExchangeDB.RecordDisconnectComponents(
                    fromInstanceGuid: fromGuid,
                    fromParameter: fromParameter,
                    toInstanceGuid: toGuid,
                    toParameter: toParameter,
                    description: $"断开组件 {fromGuid}.{fromParameter} 到 {toGuid}.{toParameter} 的连接"
                );
            }

            return result;
        }

    }

}
