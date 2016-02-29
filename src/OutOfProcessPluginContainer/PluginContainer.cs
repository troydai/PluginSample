using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin.Abstractions;

namespace OutOfProcessPluginContainer
{
    public class PluginContainer
    {
        private readonly MessageWriter _writer;
        private readonly HostContext _hostContext;
        private IServiceProvider _pluginServiceProvider;
        private readonly JsonSerializer _serializer;

        public PluginContainer(MessageWriter writer, HostContext description, JsonSerializer serializer)
        {
            _writer = writer;
            _hostContext = description;
            _serializer = serializer;
        }

        public void Initialize()
        {
            _writer.WriteMessage($"Initializing plugin");

            try
            {
                var hostProcess = Process.GetProcessById(_hostContext.HostPID);
                hostProcess.EnableRaisingEvents = true;
                hostProcess.Exited += (sender, e) =>
                {
                    Environment.Exit(0);
                };
            }
            catch
            {
                _writer.WriteMessage("Unable to attach to host process");
            }

            while (true)
            {
                var line = Console.ReadLine();
                var obj = JObject.Parse(line);

                // RPC call
                var id = obj.Value<string>("id");
                var typeName = obj.Value<string>("type");
                var method = obj.Value<string>("method");
                var methodArgs = obj.Value<JArray>("args");

                Type type = null;
                object instance = null;

                var result = new JObject();
                result["id"] = id;

                try
                {
                    if (typeName == "PluginContainer")
                    {
                        type = typeof(PluginContainer);
                        instance = this;
                    }
                    else
                    {
                        type = Type.GetType(typeName);
                        instance = _pluginServiceProvider?.GetService(type);
                    }

                    var methodInfo = type.GetMethod(method);

                    var callArgs = methodInfo.GetParameters().Select((p, i) => methodArgs[i].ToObject(p.ParameterType, _serializer)).ToArray();

                    var returnValue = methodInfo.Invoke(instance, callArgs);
                    result["result"] = methodInfo.ReturnType == typeof(void) ? null : JToken.FromObject(returnValue);
                }
                catch (Exception ex)
                {
                    result["error"] = ex.ToString();
                }

                _writer.Write(result);
            }
        }

        public void InitializeDependencies(Dictionary<string, string> dependencies)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string path;
                if (dependencies.TryGetValue(new AssemblyName(e.Name).Name, out path))
                {
                    return Assembly.LoadFile(path);
                }
                return null;
            };
        }

        public void InitializePlugin(string pluginName)
        {
            var asm = Assembly.Load(pluginName);

            var services = new ServiceCollection();
            services.AddSingleton<IPluginHost, PluginHostProxy>();
            services.AddInstance(_writer);

            foreach (var type in asm.GetExportedTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    // Add all plugins to the registry
                    services.AddTransient(typeof(IPlugin), type);
                }
            }

            _pluginServiceProvider = services.BuildServiceProvider();
        }
    }
}
