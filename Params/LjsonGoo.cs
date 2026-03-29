using Grasshopper.Kernel.Types;
using GrasshopperSever.Utils;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Storage;
using System;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输Ljson数据
    /// </summary>
    public class LjsonGoo : GH_Goo<Ljson>
    {
        /// <summary>
        /// </summary>
        public override bool IsValid => this.Value != null;

        public override string TypeName => "Ljson";

        public override string TypeDescription => "由`(DateTime time, string name，string description，JsonElement value)`组成的数据";

        public LjsonGoo()
        {
            this.Value = null;
        }

        public LjsonGoo(Ljson obj)
        {
            this.Value = obj;
        }

        public LjsonGoo(GH_String obj)
        {
            this.Value = new Ljson(obj.Value);
        }

        public override IGH_Goo Duplicate()
        {
            return new LjsonGoo(this.Value.DeepClone());
        }

        public override string ToString()
        {
            if (this.Value == null)
            {
                return "Ljson Null";
            }
            return this.Value.ToString();
        }


        /// <summary>
        /// 尝试从其他类型转换为JListGoo
        /// 支持从string（JSON格式）自动转换
        /// </summary>
        /// <param name="source">源对象</param>
        /// <returns>是否转换成功</returns>
        public override bool CastFrom(object source)
        {
            if (source == null)
            {
                this.Value = null;
                return true;
            }

            // 尝试从string转换
            if (source is string json)
            {
                try
                {
                    this.Value = new Ljson(json);
                    return true;
                }
                catch (Exception)
                {
                    // JSON解析失败，无法转换
                    return false;
                }
            }
            if (source is GH_String ghjson)
            {
                try
                {
                    this.Value = new Ljson(ghjson.Value);
                    return true;
                }
                catch (Exception)
                {
                    // JSON解析失败，无法转换
                    return false;
                }
            }

            // 尝试从JList转换
            if (source is Ljson lst)
            {
                this.Value = lst;
                return true;
            }

            // 尝试从另一个JListGoo转换
            if (source is LjsonGoo goo)
            {
                this.Value = goo.Value;
                return true;
            }

            return false;
        }
    }
}
