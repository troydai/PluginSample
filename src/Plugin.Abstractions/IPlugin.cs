using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Abstractions
{
    public interface IPlugin
    {
        IPluginData TransformData(IPluginData data);
    }
}
