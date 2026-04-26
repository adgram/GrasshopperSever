using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using GrasshopperSever.Utils;
using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;

namespace GrasshopperSever.Commands
{
    /// <summary>
    /// 用于创建常见的组件，帮助快速添加组件
    /// </summary>
    public class CommonlyParam
    {
        public static Ljson AddParamWithValue(string name, PointF point, string path, string value, string nick)
        {
            string key = name.ToLowerInvariant();

            // 特殊处理：无需创建 IGH_Param，直接返回
            if (key == "true") return MakeToggle("True");
            if (key == "false") return MakeToggle("False");
            if (key == "button") return ComponentExchange.AddComponent(new GH_ButtonObject(), point, true, nick);

            // 根据 key 创建组件
            IGH_Param component = key switch
            {
                "number" or "num" or "param_number" => new Param_Number(),
                "int" or "integer" or "param_int" or "param_integer" => new Param_Integer(),
                "bool" or "boolean" or "param_bool" or "param_boolean" => new Param_Boolean(),
                "toggle" => new GH_BooleanToggle(),
                "slider" or "numberslider" => new GH_NumberSlider(),
                "panel" or "param_panel" => new GH_Panel(),
                "text" or "string" or "param_text" or "param_string" => new Param_String(),
                "point" or "pt" or "param_pt" or "param_point" => new Param_Point(),
                "vector" or "vect" or "param_vect" => new Param_Vector(),
                "color" or "colour" or "param_color" or "param_colour" => new Param_Colour(),
                "swatch" => new GH_ColourSwatch(),
                "plane" or "param_plane" => new Param_Plane(),
                "param_line" => new Param_Line(),
                "curve" or "crv" or "param_crv" or "param_curve" => new Param_Curve(),
                "param_circle" => new Param_Circle(),
                "brep" or "param_brep" => new Param_Brep(),
                "surface" or "param_surface" => new Param_Surface(),
                "mesh" or "param_mesh" => new Param_Mesh(),
                "guid" or "param_guid" => new Param_Guid(),
                "image" or "param_image" => new GH_ImageSampler(),
                _ => null
            };

            // 如果成功创建了组件，设置参数并添加
            if (component != null)
            {
                var tag = SetParamValue(component, path, value);
                return ComponentExchange.AddComponent(component, point, tag, nick);
            }

            // 默认：按名称添加
            return ComponentExchange.AddComponentByName(name, point, nick);

            // 局部函数：创建带状态的 Toggle
            Ljson MakeToggle(string state)
            {
                var toggle = new GH_BooleanToggle();
                toggle.LoadState(state);
                return ComponentExchange.AddComponent(toggle, point, true, nick);
            }
        }


        /// 设置组件的值
        /// </summary>
        public static bool SetParamValue(string instanceTag, string path, string value)
        {
            Exception caughtException = null;
            string componentName = null;
            bool tag = false;

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    // 查找组件
                    var component = ComponentExchange.FindObject(null, instanceTag);

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
                        caughtException = new InvalidOperationException($"Failed to find component with instance GUID: {instanceTag}");
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
                    instanceGuid: instanceTag,
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
            if (item is GH_ImageSampler image)
            {
                image.ImageFilePath = value;
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
                    dataList = [value];
                }
            }
            else
            {
                // 如果不是列表格式，封装为单元素列表
                dataList = [value];
            }

            // 设置参数值
            return SetParamList(item, pathp, dataList);
        }

        public static bool SetParamList(IGH_Param item, GH_Path path, List<string> datalist)
        {
            if (path.Length == 0) path = new GH_Path(0);

            // switch 表达式
            return item switch
            {
                Param_Number p => AddList(p, path, datalist),
                Param_Integer p => AddList(p, path, datalist),
                Param_Boolean p => AddList(p, path, datalist),
                Param_String p => AddList(p, path, datalist),
                Param_Point p => AddList(p, path, datalist),
                Param_Vector p => AddList(p, path, datalist),
                Param_Colour p => AddList(p, path, datalist),
                Param_Surface p => AddConvertedList(p, path, datalist, s => new GH_Surface(Guid.Parse(s))),
                Param_Curve p => AddConvertedList(p, path, datalist, s => new GH_Curve(Guid.Parse(s))),
                Param_Brep p => AddConvertedList(p, path, datalist, s => new GH_Brep(Guid.Parse(s))),
                Param_Mesh p => AddConvertedList(p, path, datalist, s => new GH_Mesh(Guid.Parse(s))),
                Param_Guid p => AddConvertedList(p, path, datalist, Guid.Parse),
                _ => false
            };
        }

        // 辅助方法：直接传递 datalist
        private static bool AddList(IGH_Param param, GH_Path path, List<string> datalist)
        {
            param.AddVolatileDataList(path, datalist);
            return true;
        }

        // 辅助方法：需要转换 datalist 元素
        private static bool AddConvertedList<T>(IGH_Param param, GH_Path path, List<string> datalist, Func<string, T> converter)
        {
            var converted = datalist.Select(converter).ToList();
            param.AddVolatileDataList(path, converted);
            return true;
        }

        private static List<Guid> GuidFromString(IEnumerable<string> datalist)
        {
            return datalist.Select(str => Guid.Parse(str)).ToList();
        }
    }
}
