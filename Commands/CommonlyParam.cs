using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
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
                    return ComponentExchange.AddComponent(truep, point, true);
                case "false":
                    var falsep = new GH_BooleanToggle();
                    falsep.LoadState("False");
                    return ComponentExchange.AddComponent(falsep, point, true);
                case "toggle":
                    component = new GH_BooleanToggle();
                    break;
                case "button":
                    var buttonp = new GH_ButtonObject();
                    // 无需传入值
                    return ComponentExchange.AddComponent(buttonp, point, true);
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
                case "brep":
                case "param_brep":
                    component = new Param_Brep();
                    break;
                case "surface":
                case "param_surface":
                    component = new Param_Surface();
                    break;
                case "mesh":
                case "param_mesh":
                    component = new Param_Mesh();
                    break;
                case "guid":
                case "param_guid":
                    component = new Param_Guid();
                    break;
                default:
                    return ComponentExchange.AddComponentByName(name, point);
            }
            var tag = SetParamValue(component, path, value);
            var lj = ComponentExchange.AddComponent(component, point, tag);
            return lj;
        }
        
        
        /// 设置组件的值
        /// </summary>
        public static bool SetParamValue(string guid, string path, string value)
        {
            Exception caughtException = null;
            string componentName = null;
            bool tag = false;

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
                            componentName = item.Name;
                            tag = SetParamValue(item, path, value);
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
            if (tag)
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

        public static bool SetParamValue(IGH_Param item, string path, string value)
        {
            if (item is GH_BooleanToggle togglep)
            {
                togglep.LoadState(value);
                return true;
            }
            if (item is GH_NumberSlider slider)
            {
                // 样式： "0.0 < 0.5 < 1.0"
                slider.SetInitCode(value);
                return true;
            }
            if (item is GH_Panel panelp)
            {
                // 支持换行，但是默认值只能是item，不能是list
                panelp.SetUserText(value);
                return true;
            }
            if (item is GH_ColourSwatch swatch)
            {
                swatch.LoadState(value);
                return true;
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
        
        public static bool SetParamList(IGH_Param item, GH_Path path, IEnumerable<string> datalist)
        {
            if (path.Length == 0)
            {
                path = new GH_Path(0);
            }
            if (item is Param_Number nump)
            {
                nump.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_Integer intp)
            {
                intp.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_Boolean boolp)
            {
                boolp.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_String textp)
            {
                textp.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_Point pointp)
            {
                pointp.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_Vector vectp)
            {
                vectp.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_Colour colorp)
            {
                colorp.AddVolatileDataList(path, datalist);
                return true;
            }
            if (item is Param_Surface surfp)
            {
                List<GH_Surface> gHs = new();
                foreach (string str in datalist)
                {

                    gHs.Add(new GH_Surface(Guid.Parse(str)));
                }
                surfp.AddVolatileDataList(path, gHs);
                return true;
            }
            if (item is Param_Curve cuvp)
            {
                List<GH_Curve> gHs = new();
                foreach (string str in datalist)
                {

                    gHs.Add(new GH_Curve(Guid.Parse(str)));
                }
                cuvp.AddVolatileDataList(path, gHs);
                return true;
            }
            if (item is Param_Brep brep)
            {
                List<GH_Brep> gHs = new();
                foreach (string str in datalist)
                {

                    gHs.Add(new GH_Brep(Guid.Parse(str)));
                }
                brep.AddVolatileDataList(path, gHs);
                return true;
            }
            if (item is Param_Mesh msp)
            {
                List<GH_Mesh> gHs = new();
                foreach (string str in datalist)
                {

                    gHs.Add(new GH_Mesh(Guid.Parse(str)));
                }
                msp.AddVolatileDataList(path, gHs);
                return true;
            }
            if (item is Param_Guid idp)
            {
                List<Guid> gHs = new();
                foreach (string str in datalist)
                {

                    gHs.Add(Guid.Parse(str));
                }
                idp.AddVolatileDataList(path, gHs);
                return true;
            }
            return false;
        }

        public static List<Guid> GuidFromString(IEnumerable<string> datalist)
        {
            List<Guid> guids = new();
            foreach (string str in datalist)
            {
                guids.Add(Guid.Parse(str));  // 无效时会抛出 FormatException
            }
            return guids;
        }
    }
}
