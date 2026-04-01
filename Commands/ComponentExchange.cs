using Grasshopper.Kernel;
using GrasshopperSever.Utils;
using System.Collections.Generic;
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
            string category, string subCategory, string position,
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
    }
    
}
