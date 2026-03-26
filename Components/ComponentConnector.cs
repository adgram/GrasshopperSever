using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace GrasshopperSever.Components
{
    public class ComponentConnector : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ComponentConnector class.
        /// </summary>
        public ComponentConnector()
          : base("ComponentConnector", "CompConn",
              "获取连接到的组件对象",
              "Math", "Sever")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Input", "I", "连接一个组件", GH_ParamAccess.tree);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "组件名字", GH_ParamAccess.list);
            pManager.AddTextParameter("GUID", "ID", "组件的GUID", GH_ParamAccess.list);
            pManager.AddTextParameter("Instance", "TS", "组件对象的GUID", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get the connected components
            var connectedComponents = GetConnectedComponents();

            if (connectedComponents.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No connected components found");
                return;
            }

            // Extract component information
            List<string> names = new List<string>();
            List<string> ids = new List<string>();
            List<string> ints = new List<string>();

            foreach (var component in connectedComponents)
            {
                names.Add(component.Name);
                ids.Add(component.ComponentGuid.ToString());
                ints.Add(component.InstanceGuid.ToString());
            }

            // Set output
            DA.SetDataList(0, names);
            DA.SetDataList(1, ids);
            DA.SetDataList(2, ints);
        }

        private List<IGH_DocumentObject> GetConnectedComponents()
        {
            List<IGH_DocumentObject> connectedComponents = new List<IGH_DocumentObject>();
            foreach (var source in Params.Input[0].Sources)
            {
                IGH_DocumentObject sourceComponent = null;

                // Get the parent component of the source
                if (source is IGH_Param sourceParam)
                {
                    sourceComponent = sourceParam.Attributes.GetTopLevel.DocObject;
                }

                if (sourceComponent != null && !connectedComponents.Contains(sourceComponent))
                {
                    connectedComponents.Add(sourceComponent);
                }
            }
            return connectedComponents;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.P15_ComponentConnector;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("2ED7C554-105B-438E-ADF3-743168B3E8E7");
    }
}