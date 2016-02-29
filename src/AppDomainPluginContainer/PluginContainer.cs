using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Abstractions;

namespace AppDomainPluginContainer
{
    public class PluginContainer : MarshalByRefObject, IPlugin
    {
        private IPlugin _plugin;

        public void InitializeDependencies(Dictionary<string, string> dependencies)
        {
            // Load the project context, figure out runtime dependencies, configure the environment to load them
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string path;
                if (dependencies.TryGetValue(new AssemblyName(args.Name).Name, out path))
                {
                    return Assembly.LoadFile(path);
                }
                return null;
            };
        }

        public void Initialize(string pluginName, IPluginHost host)
        {
            var asm = Assembly.Load(pluginName);

            var services = new ServiceCollection();
            services.AddInstance(host);

            foreach (var type in asm.GetExportedTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    // Add all plugins to the registry
                    services.AddTransient(typeof(IPlugin), type);
                }
            }

            // Activate
            var sp = services.BuildServiceProvider();
            _plugin = sp.GetService<IPlugin>();
        }

        public IPluginData TransformData(IPluginData data)
        {
            return Data.Wrap(_plugin.TransformData(data));
        }
    }
}