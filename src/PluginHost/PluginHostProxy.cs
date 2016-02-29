using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Abstractions;

namespace OutOfProcessPluginContainer
{
    public class PluginHostProxy : IPluginHost
    {
        private readonly MessageWriter _writer;
        public PluginHostProxy(MessageWriter writer)
        {
            _writer = writer;
        }
        public void Callback(IPluginData data)
        {
            _writer.Write(new
            {
                method = "Callback",
                type = typeof(IPluginHost).AssemblyQualifiedName,
                args = new[] { data }
            });
        }
    }
}
