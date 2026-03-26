using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using Rhino.Geometry;

namespace GrasshopperSever.Components
{
    public class SearchDataBase : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SearchDataBase class.
        /// </summary>
        public SearchDataBase()
          : base("SearchDataBase", "SearchDB",
              "查询数据库",
              "Maths", "Sever")
        {
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.secondary;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SQL", "SQL", "完整的SQL查询语句", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "查询结果，以JSON格式返回", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string sql = string.Empty;

            // 获取输入参数
            if (!DA.GetData(0, ref sql)) return;

            if (string.IsNullOrWhiteSpace(sql))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "SQL语句不能为空");
                return;
            }

            using (var connection = DatabaseManager.GetConnection())
            {
                var results = new List<string>();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 动态获取所有列名和值
                            var rowData = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnName = reader.GetName(i);
                                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                rowData[columnName] = value;
                            }
                            results.Add(System.Text.Json.JsonSerializer.Serialize(rowData));
                        }
                    }
                }

                // 将结果组合为JSON数组
                string resultJson = $"[{string.Join(",", results)}]";
                DA.SetData(0, resultJson);
            }
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.P19_SearchDataBase;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CA048A45-E2E3-47CD-AD5F-AEE6250C52FF"); }
        }
    }
}