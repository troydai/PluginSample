using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DomainPluginHost;
using Microsoft.DotNet.ProjectModel;
using NuGet.Frameworks;
using Plugin.Abstractions;

namespace AppDomainPluginHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var sourcesBaseDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            var configuration = "Debug";

            // Read the plugins from the manifest
            var plugins = ReadPluginManifest(sourcesBaseDirectory);

            var host = new PluginHost();

            // Discover all plugin types
            foreach (var pd in plugins)
            {
                // We're going to load the plugin host, so setup the base directory so we can load it
                var setup = new AppDomainSetup
                {
                    ApplicationBase = Path.Combine(sourcesBaseDirectory, "DomainPluginHost", "bin", configuration)
                };

                // The plugin host needs to be able to load any dependency from the host
                var dependencies = new Dictionary<string, string>();
                foreach (var file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    dependencies[name] = file;
                }

                foreach (var file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.exe"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    dependencies[name] = file;
                }

                // The plugin container needs to prefer the plugin's dependencies
                var context = ProjectContext.Create(pd.Path, NuGetFramework.Parse("net451"));
                var pluginDependencies = context.CreateExporter(configuration).GetAllExports().SelectMany(a => a.RuntimeAssemblies)
                    .Select(a => a.ResolvedPath)
                    .ToDictionary(d => Path.GetFileNameWithoutExtension(d));

                foreach (var dependency in pluginDependencies)
                {
                    dependencies[dependency.Key] = dependency.Value;
                }

                // Create the plugin app domain
                var pluginDomain = AppDomain.CreateDomain(pd.Name, AppDomain.CurrentDomain.Evidence, setup);
                var plugin = (PluginContainer)pluginDomain.CreateInstanceAndUnwrap(typeof(PluginContainer).Assembly.FullName, typeof(PluginContainer).FullName);
                plugin.InitializeDependencies(dependencies);
                plugin.Initialize(pd.Name, host);

                var data = plugin.TransformData(new Data
                {
                    Message = "David"
                });

                Console.WriteLine(data.Message);
            }
        }

        private static PluginDescription[] ReadPluginManifest(string sourcesBaseDirectory)
        {
            // TODO: Read this from the manifest or scan all assemblies (eww)
            return new[] {
                new PluginDescription {
                     Name = "Plugin1",
                     Path = Path.Combine(sourcesBaseDirectory, "Plugin1")
                },
                new PluginDescription {
                     Name = "Plugin2",
                     Path = Path.Combine(sourcesBaseDirectory, "Plugin2")
                }
            };
        }
    }

    [Serializable]
    public class Data : IPluginData
    {
        public string Message { get; set; }
    }

    public class PluginDescription
    {
        public string Path { get; set; }
        public string Name { get; set; }
    }
}
