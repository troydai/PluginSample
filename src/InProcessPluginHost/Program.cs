using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Abstractions;

namespace InProcessPluginHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Read the plugins from the manifest
            var plugins = ReadPluginManifest();

            // The service collection will add all plugins and services that plugins have access to
            var services = new ServiceCollection();
            services.AddTransient<IPluginHost, PluginHost>();

            // Discover all plugin types
            foreach (var pluginName in plugins)
            {
                var asm = Assembly.Load(new AssemblyName(pluginName));
                foreach (var type in asm.GetExportedTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type))
                    {
                        // Add all plugins to the registry
                        services.AddTransient(typeof(IPlugin), type);
                    }
                }
            }

            var data = new PluginData
            {
                Message = "David"
            };

            // Activate
            var sp = services.BuildServiceProvider();
            foreach (var plugin in sp.GetServices<IPlugin>())
            {
                Console.WriteLine("{0}: {1}", plugin.GetType(), plugin.TransformData(data).Message);
            }
        }

        private static string[] ReadPluginManifest()
        {
            // TODO: Read this from the manifest or scan all assemblies (eww)
            return new[] { "Plugin1", "Plugin2" };
        }
    }

    public class PluginData : IPluginData
    {
        public string Message { get; set; }
    }

}
