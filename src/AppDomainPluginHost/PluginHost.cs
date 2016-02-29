using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Abstractions;

namespace AppDomainPluginHost
{

    [Serializable]
    public class PluginHost : IPluginHost
    {
        public void Callback(IPluginData data)
        {
            Console.WriteLine($"Callback {data.Message}");
        }
    }

}
