using System;
using Plugin.Abstractions;

namespace InProcessPluginHost
{
    internal class PluginHost : IPluginHost
    {
        public void Callback(IPluginData data)
        {
            Console.WriteLine($"Callback {data.Message}");
        }
    }
}