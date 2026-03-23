using Grasshopper.Kernel.Types;
using GrasshopperSever.Utils;
using System;

namespace GrasshopperSever.Params
{
    /// <summary>
    /// 定义电池端口，这个传输JList数据
    /// </summary>
    public class JListGoo : GH_Goo<JList>
    {
        /// <summary>
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return this.Value != null;
            }
        }

        public override string TypeName
        {
            get
            {
                return "JList";
            }
        }

        public override string TypeDescription
        {
            get
            {
                return "由`(DateTime time, List<JData(string name，string description，string data)>)`组成的数据";
            }
        }

        public JListGoo()
        {
            this.Value = null;
        }

        public JListGoo(JList obj)
        {
            this.Value = obj;
        }

        public override IGH_Goo Duplicate()
        {
            return new JListGoo(this.Value.DeepClone());
        }

        public override string ToString()
        {
            if (this.Value == null)
            {
                return "JList Null";
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
                    this.Value = new JList(json);
                    return true;
                }
                catch (Exception)
                {
                    // JSON解析失败，无法转换
                    return false;
                }
            }

            // 尝试从JList转换
            if (source is JList lst)
            {
                this.Value = lst;
                return true;
            }

            // 尝试从另一个JListGoo转换
            if (source is JListGoo goo)
            {
                this.Value = goo.Value;
                return true;
            }

            return false;
        }
    }
}
