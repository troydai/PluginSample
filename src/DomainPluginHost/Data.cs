using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Abstractions;

namespace DomainPluginHost
{

    /// <summary>
    /// Serializable version of IPluginData, the plugin doesn't know that it's running in a separate app domain
    /// and the models are defined as interfaces. We decorate the original IPluginData if it's not 
    /// Serializable.
    /// </summary>
    [Serializable]
    internal class Data : IPluginData
    {
        public Data(IPluginData data)
        {
            Message = data.Message;
        }

        public string Message { get; set; }

        public static IPluginData Wrap(IPluginData data)
        {
            if (data.GetType().IsSerializable)
            {
                return data;
            }
            return new Data(data);
        }
    }
}
