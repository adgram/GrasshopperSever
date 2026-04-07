using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
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
        public static Ljson ComponentLjson(string componentGuid, string instanceGuid,
            string name, string nickName, string description,
            string category, string subCategory, PointF position,
            string state, string inputs, string outputs)
        {
            var data = new Dictionary<string, JsonElement>
            {
                { "ComponentGuid", JsonSerializer.SerializeToElement(componentGuid) },
                { "InstanceGuid", JsonSerializer.SerializeToElement(instanceGuid) },
                { "ComponentName", JsonSerializer.SerializeToElement(name) },
                { "NickName", JsonSerializer.SerializeToElement(nickName) },
                { "Description", JsonSerializer.SerializeToElement(description) },
                { "Category", JsonSerializer.SerializeToElement(category) },
                { "SubCategory", JsonSerializer.SerializeToElement(subCategory) },
                { "Position", JsonSerializer.SerializeToElement(position) },
                { "State", JsonSerializer.SerializeToElement(state) },
                { "Inputs", JsonSerializer.SerializeToElement(inputs) },
                { "Outputs", JsonSerializer.SerializeToElement(outputs) }
            };

            return new Ljson("Component", "组件信息", JsonSerializer.SerializeToElement(data));
        }

        /// <summary>
        /// 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponent(Ljson ljson, PointF point)
        {
            var componentGuid = ljson.GetParameterString("ComponentGuid");
            Ljson result = ljson.DeepClone();
            Exception caughtException = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");
                    IGH_DocumentObject dobj = Instances.ComponentServer.EmitObject(new Guid(componentGuid));
                    
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

                        // 创建返回的 Ljson
                        result.SetParameter("InstanceGuid", JsonSerializer.SerializeToElement(dobj.InstanceGuid.ToString()));
                        result.SetParameter("Position", JsonSerializer.SerializeToElement(point));
                    }
                    else
                    {
                        caughtException = new InvalidOperationException($"Failed to create component with GUID: {componentGuid}");
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

            return result;
        }

        /// <summary>
        /// 通过组件 GUID 添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByGuid(string cguid, PointF point)
        {
            Ljson ljson = ComponentInfo.FindComponentsByGuid(cguid);
            if(ljson != null) return AddComponent(ljson, point);
            
            throw new InvalidOperationException($"Failed to find component with GUID: {cguid}");
        }

        /// <summary>
        /// 通过组件名称添加组件到 Grasshopper 文档
        /// </summary>
        public static Ljson AddComponentByName(string name, PointF point)
        {
            Ljson ljson = ComponentInfo.FindComponentsByName(name);
            if (ljson != null) return AddComponent(ljson, point);
            
            throw new InvalidOperationException($"Failed to find component with name: {name}");
        }

        /// <summary>
        /// 从 Grasshopper 文档中移除组件
        /// </summary>
        public static bool RemoveComponent(string guid)
        {
            bool success = false;
            Exception caughtException = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");

                    // 查找组件
                    var component = doc.FindObject(new Guid(guid), false);
                    
                    if (component != null)
                    {
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

            return success;
        }

        /// <summary>
        /// 设置组件的值
        /// </summary>
        public static bool SetComponentValue(string guid, string value)
        {
            bool success = false;
            Exception caughtException = null;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var doc = (Instances.ActiveCanvas?.Document) ?? throw new InvalidOperationException("No active Grasshopper document");

                    // 查找组件
                    var component = doc.FindObject(new Guid(guid), false);

                    if (component != null)
                    {
                        if (component is GH_Panel panel)
                        {
                            panel.UserText = value;
                        }
                        else if (component is GH_NumberSlider slider)
                        {
                            double doubleValue;
                            if (double.TryParse(value, out doubleValue))
                            {
                                slider.SetSliderValue((decimal)doubleValue);
                            }
                            else
                            {
                                throw new ArgumentException("Invalid slider value format");
                            }
                        }
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

            return success;
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
                    
                    if (!Guid.TryParse(fromGuid, out Guid fromId) || !Guid.TryParse(toGuid, out Guid toId))
                    {
                        throw new ArgumentException("Invalid component ID format");
                    }

                    if (doc.FindComponent(fromId) is not IGH_Component fromComponent || 
                        doc.FindComponent(toId) is not IGH_Component toComponent)
                    {
                        throw new ArgumentException("Source or target component not found");
                    }

                    IGH_Param fromParam = null;
                    foreach (var param in fromComponent.Params.Output)
                    {
                        if (param.Name.Equals(fromParameter, StringComparison.OrdinalIgnoreCase))
                        {
                            fromParam = param;
                            break;
                        }
                    }

                    IGH_Param toParam = null;
                    foreach (var param in toComponent.Params.Input)
                    {
                        if (param.Name.Equals(toParameter, StringComparison.OrdinalIgnoreCase))
                        {
                            toParam = param;
                            break;
                        }
                    }

                    if (fromParam == null || toParam == null)
                    {
                        throw new ArgumentException("Source or target parameter not found");
                    }

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
                    
                    if (!Guid.TryParse(fromGuid, out Guid fromId) || !Guid.TryParse(toGuid, out Guid toId))
                    {
                        throw new ArgumentException("Invalid component ID format");
                    }

                    if (doc.FindComponent(fromId) is not IGH_Component fromComponent || 
                        doc.FindComponent(toId) is not IGH_Component toComponent)
                    {
                        throw new ArgumentException("Source or target component not found");
                    }

                    IGH_Param fromParam = null;
                    foreach (var param in fromComponent.Params.Output)
                    {
                        if (param.Name.Equals(fromParameter, StringComparison.OrdinalIgnoreCase))
                        {
                            fromParam = param;
                            break;
                        }
                    }

                    IGH_Param toParam = null;
                    foreach (var param in toComponent.Params.Input)
                    {
                        if (param.Name.Equals(toParameter, StringComparison.OrdinalIgnoreCase))
                        {
                            toParam = param;
                            break;
                        }
                    }

                    if (fromParam == null || toParam == null)
                    {
                        throw new ArgumentException("Source or target parameter not found");
                    }

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

            return result;
        }

    }

}
