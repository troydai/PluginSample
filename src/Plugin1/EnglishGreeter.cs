using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Plugin.Abstractions;

namespace Plugin1
{
    public class EnglishGreeter : IPlugin
    {
        public EnglishGreeter(IPluginHost host)
        {
            host.Callback(new PluginData { Message = typeof(EnglishGreeter).FullName });
        }

        public IPluginData TransformData(IPluginData data)
        {
            return new PluginData { Message = $"Hello {data.Message}" };
        }

        private class PluginData : IPluginData
        {
            public string Message { get; set; }
        }
    }
}
