using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace GrasshopperSever.Commands
{
    public class ComponentExchange
    {
        private static Dictionary<Guid, Dictionary<string, IGH_DocumentObject>> UserObj = [];

        private static void AddComponentToDocument(IGH_DocumentObject dobj, PointF point, string nick)
        {
            var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");
            // 确保有属性
            if (dobj.Attributes == null)
            {
                dobj.CreateAttributes();
            }
            if (!string.IsNullOrEmpty(nick))
            {
                dobj.NickName = nick;
                if (!UserObj.TryGetValue(doc.DocumentID, out Dictionary<string, IGH_DocumentObject> value))
                {
                    value = [];
                    UserObj[doc.DocumentID] = value;
                }
                value[nick] = dobj;
            }
            // 设置位置
            dobj.Attributes.Pivot = point;
            doc.AddObject(dobj, false);
            doc.NewSolution(false);
        }

        /// <summary>
        /// 通过组件 GUID 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByGuid(string guid, PointF point, string nick)
        {
            Exception caughtException = null;
            IGH_DocumentObject dobj = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    dobj = Instances.ComponentServer.EmitObject(new Guid(guid));
                    if (dobj != null)
                    {
                        AddComponentToDocument(dobj, point, nick);
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

        public static Ljson AddComponent(IGH_DocumentObject dobj, PointF point, bool hasCustomValue, string nick)
        {
            if (dobj == null) throw new InvalidOperationException("Failed to create component");
            Exception caughtException = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    AddComponentToDocument(dobj, point, nick);
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

            return RecordAddComponent(dobj, point, hasCustomValue);
        }

        /// <summary>
        /// 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson RecordAddComponent(IGH_DocumentObject component, PointF point, bool hasCustomValue = false)
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
            // 返回 InstanceLjson
            return ComponentInfo.InstanceLjsonBrief(component, point, hasCustomValue);
        }


        /// <summary>
        /// 通过组件名称添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByName(string name, PointF point, string nick)
        {
            string guid = ComponentInfo.FindComponentsGuidByName(name);
            if (guid != null) return AddComponentByGuid(guid, point, nick);
            return null;
        }

        public static IGH_DocumentObject FindObject(GH_Document doc, string guidOrNick)
        {
            doc ??= (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");
            if (UserObj != null && UserObj.TryGetValue(doc.DocumentID, out Dictionary<string, IGH_DocumentObject> ds)
                && ds != null && ds.TryGetValue(guidOrNick, out IGH_DocumentObject value))
            {
                return value;
            }
            if (Guid.TryParse(guidOrNick, out Guid uid))
            {
                return doc.FindObject(uid, false);
            }
            return null;
        }

        /// <summary>
        /// 从 Grasshopper 文档中移除组件
        /// </summary>
        public static bool RemoveComponent(string guidOrNick)
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
                    var component = FindObject(doc, guidOrNick);

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
                        caughtException = new InvalidOperationException($"Failed to find component with instance: {guidOrNick}");
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
                    instanceGuid: guidOrNick,
                    componentName: componentName,
                    description: $"删除组件 {componentName}"
                );
            }

            return success;
        }

        public static IGH_Param FindParam(GH_Document doc, string guidOrNick, string name, bool isIn)
        {
            var obj = FindObject(doc, guidOrNick);
            if (obj is IGH_Param p)
            {
                return p;
            }
            if (obj is IGH_Component component)
            {
                List<IGH_Param> ps = component.Params.Output;
                if (isIn) ps = component.Params.Input;

                foreach (var param in ps)
                {
                    if (param.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return param;
                    }
                }
            }
            throw new ArgumentException($"Source or target {guidOrNick} not found");
        }

        /// <summary>
        /// 连接两个组件的参数
        /// </summary>
        /// <param name="fromTag">源组件的实例 GUID</param>
        /// <param name="fromParameter">源组件的输出参数名称</param>
        /// <param name="toTag">目标组件的实例 GUID</param>
        /// <param name="toParameter">目标组件的输入参数名称</param>
        /// <returns>连接是否成功</returns>
        public static bool ConnectComponents(string fromTag, string fromParameter, string toTag, string toParameter)
        {
            bool result = false;
            Exception exception = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");

                    var fromParam = FindParam(doc, fromTag, fromParameter, false);
                    var toParam = FindParam(doc, toTag, toParameter, true);

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
                    fromInstanceGuid: fromTag,
                    fromParameter: fromParameter,
                    toInstanceGuid: toTag,
                    toParameter: toParameter,
                    description: $"连接组件 {fromTag}.{fromParameter} 到 {toTag}.{toParameter}"
                );
            }

            return result;
        }

        /// <summary>
        /// 断开两个组件参数之间的连接
        /// </summary>
        /// <param name="fromTag">源组件的实例 GUID</param>
        /// <param name="fromParameter">源组件的输出参数名称</param>
        /// <param name="toTag">目标组件的实例 GUID</param>
        /// <param name="toParameter">目标组件的输入参数名称</param>
        /// <returns>断开连接是否成功</returns>
        public static bool DisconnectComponents(string fromTag, string fromParameter, string toTag, string toParameter)
        {
            bool result = false;
            Exception exception = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");


                    var fromParam = FindParam(doc, fromTag, fromParameter, false);
                    var toParam = FindParam(doc, toTag, toParameter, true);

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
                    fromInstanceGuid: fromTag,
                    fromParameter: fromParameter,
                    toInstanceGuid: toTag,
                    toParameter: toParameter,
                    description: $"断开组件 {fromTag}.{fromParameter} 到 {toTag}.{toParameter} 的连接"
                );
            }

            return result;
        }

    }

}
