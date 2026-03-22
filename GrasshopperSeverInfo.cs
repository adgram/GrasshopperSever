using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using GrasshopperSever.Utils;

namespace GrasshopperSever
{
    public class GrasshopperSeverInfo : GH_AssemblyInfo
    {
        /// <summary>
        /// 构造函数 - 插件加载时自动初始化数据库
        /// </summary>
        public GrasshopperSeverInfo()
        {
            // 初始化数据库系统
            DatabaseManager.Initialize();
        }

        public override string Name => "GrasshopperSever";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("0171a275-7e22-4b2a-9f82-b80f07a08b08");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}