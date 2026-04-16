using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using GrasshopperSever.Utils;
using Rhino;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    /// <summary>
    /// 用于创建常见的组件，帮助快速添加组件
    /// </summary>
    public class CommonlyParam
    {
        public static Ljson AddParamWithValue(string name, PointF point, string path, string value)
        {
            IGH_Param component;
            switch (name.ToLowerInvariant())
            {
                case "number":
                case "num":
                case "param_number":
                    component = new Param_Number();
                    break;
                case "int":
                case "integer":
                case "param_int":
                case "param_integer":
                    component = new Param_Integer();
                    break;
                case "bool":
                case "boolean":
                case "param_bool":
                case "param_boolean":
                    component = new Param_Boolean();
                    break;
                case "true":
                    var truep = new GH_BooleanToggle();
                    truep.LoadState("True");
                    return ComponentExchange.AddComponent(truep, point);
                case "false":
                    var falsep = new GH_BooleanToggle();
                    falsep.LoadState("False");
                    return ComponentExchange.AddComponent(falsep, point);
                case "toggle":
                    component = new GH_BooleanToggle();
                    break;
                case "button":
                    var buttonp = new GH_ButtonObject();
                    // 无需传入值
                    return ComponentExchange.AddComponent(buttonp, point);
                case "slider":
                case "numberslider":
                    component = new GH_NumberSlider();
                    break;
                case "panel":
                case "param_panel":
                    component = new GH_Panel();
                    break;
                case "text":
                case "string":
                case "param_text":
                case "param_string":
                    component = new Param_String();
                    break;
                case "point":
                case "pt":
                case "param_pt":
                case "param_point":
                    component = new Param_Point();
                    break;
                case "vector":
                case "vect":
                case "param_vect":
                    component = new Param_Vector();
                    break;
                case "color":
                case "colour":
                case "param_color":
                case "param_colour":
                    component = new Param_Colour();
                    break;
                case "swatch":
                    component = new GH_ColourSwatch();
                    break;
                // 下面几个无法设置参数，但为常见组件，这里提供便捷添加
                case "plane":
                case "param_plane":
                    component = new Param_Plane();
                    break;
                case "param_line":
                    component = new Param_Line();
                    break;
                case "curve":
                case "crv":
                case "param_crv":
                case "param_curve":
                    component = new Param_Curve();
                    break;
                case "param_circle":
                    component = new Param_Circle();
                    break;
                default:
                    return null;
            }
            SetParamValue(component, path, value);
            return ComponentExchange.AddComponent(component, point);
        }
        
        
        /// 设置组件的值
        /// </summary>
        public static bool SetParamValue(string guid, string path, string value)
        {
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
                        if (component is IGH_Param item)
                        {
                            componentName = SetParamValue(item, path, value);
                        }
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

            // 记录设置组件值操作到数据库
            if (!string.IsNullOrEmpty(componentName))
            {
                ComponentExchangeDB.RecordSetComponentValue(
                    instanceGuid: guid,
                    componentName: componentName,
                    value: value,
                    description: $"设置组件 {componentName} 的值为 {value}"
                );
                return true;
            }

            return false;
        }

        public static string SetParamValue(IGH_Param item, string path, string value)
        {
            if (item is GH_BooleanToggle togglep)
            {
                togglep.LoadState(value);
                return togglep.Name;
            }
            if (item is GH_NumberSlider slider)
            {
                // 样式： "0.0 < 0.5 < 1.0"
                slider.SetInitCode(value);
                return slider.Name;
            }
            if (item is GH_Panel panelp)
            {
                // 支持换行，但是默认值只能是item，不能是list
                panelp.SetUserText(value);
                return panelp.Name;
            }
            if (item is GH_ColourSwatch swatch)
            {
                swatch.LoadState(value);
                return swatch.Name;
            }
            /*
            // 示例数据结构
            string jsonData = @"{
              ""Path"": ""{0;1;2}"",
              ""Values"": [1.0, 2.0, 3.0]
            }";
             */
            // 解析 JSON
            var pathp = new GH_Path();
            pathp.FromString(path);
            
            // 提取数值列表
            List<string> dataList;
            if (value.TrimStart().StartsWith("["))
            {
                // 如果字符串以 [ 开头，尝试反序列化为列表
                try
                {
                    dataList = JsonSerializer.Deserialize<List<string>>(value);
                }
                catch
                {
                    // 如果反序列化失败，封装为单元素列表
                    dataList = new List<string> { value };
                }
            }
            else
            {
                // 如果不是列表格式，封装为单元素列表
                dataList = new List<string> { value };
            }
            
            // 设置参数值
            return SetParamList(item, pathp, dataList);
        }
        
        public static string SetParamList(IGH_Param item, GH_Path path, IEnumerable datalist)
        {
            if (path.Length == 0)
            {
                path = new GH_Path(0);
            }
            if (item is Param_Number nump)
            {
                nump.AddVolatileDataList(path, datalist);
                return nump.Name;
            }
            if (item is Param_Integer intp)
            {
                intp.AddVolatileDataList(path, datalist);
                return intp.Name;
            }
            if (item is Param_Boolean boolp)
            {
                boolp.AddVolatileDataList(path, datalist);
                return boolp.Name;
            }
            if (item is Param_String textp)
            {
                textp.AddVolatileDataList(path, datalist);
                return textp.Name;
            }
            if (item is Param_Point pointp)
            {
                pointp.AddVolatileDataList(path, datalist);
                return pointp.Name;
            }
            if (item is Param_Vector vectp)
            {
                vectp.AddVolatileDataList(path, datalist);
                return vectp.Name;
            }
            if (item is Param_Colour colorp)
            {
                colorp.AddVolatileDataList(path, datalist);
                return colorp.Name;
            }
            return null;
        }
    }
}
