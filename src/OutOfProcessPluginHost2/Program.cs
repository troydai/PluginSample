using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NuGet.Frameworks;
using Plugin.Abstractions;

namespace OutOfProcessPluginHost
{
    public class PluginHost : IPluginHost
    {
        public void Callback(IPluginData data)
        {
            Console.WriteLine($"Callback {data.Message}");
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var sourcesDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            var configuration = "Debug";
            var pluginHost = Path.Combine(sourcesDirectory, "PluginHost", "bin", configuration, "PluginHost.exe");
            var plugins = ReadPluginManifest(sourcesDirectory);

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new PluginDataConverter());

            var services = new ServiceCollection();
            services.AddTransient<IPluginHost, PluginHost>();
            services.AddInstance(serializer);
            var sp = services.BuildServiceProvider();

            // Discover all plugin types
            foreach (var pd in plugins)
            {
                var context = ProjectContext.Create(pd.Path, NuGetFramework.Parse("net451"));
                var pluginDependencies = context.CreateExporter(configuration).GetAllExports().SelectMany(a => a.RuntimeAssemblies)
                    .Select(a => a.ResolvedPath)
                    .ToDictionary(d => Path.GetFileNameWithoutExtension(d));

                var plugin = new PluginContainer(pluginHost, pd, sp);
                plugin.Initialize(pluginDependencies);

                var data = plugin.TransformData(new PluginData
                {
                    Message = "David"
                });

                Console.WriteLine(data.Message);
            }
        }

        private static PluginDescription[] ReadPluginManifest(string sourcesDirectory)
        {
            // TODO: Read this from the manifest or scan all assemblies (eww)
            return new[] {
                new PluginDescription {
                     Name = "Plugin1",
                     Path = Path.Combine(sourcesDirectory,"Plugin1")
                },
                new PluginDescription {
                     Name = "Plugin2",
                     Path = Path.Combine(sourcesDirectory,"Plugin2")
                }
            };
        }
    }

    public class PluginDescription
    {
        public string Path { get; set; }
        public string Name { get; set; }
    }
}
