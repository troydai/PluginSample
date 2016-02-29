using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Plugin.Abstractions;

namespace OutOfProcessPluginHost
{
    /// <summary>
    /// Remote representation of the plugin container in the host. It's used to communicate with the plugins hosted out of process
    /// </summary>
    public class PluginContainer : IPlugin
    {
        private readonly Process _process;
        private readonly PluginDescription _pluginDescriptor;
        private readonly IServiceProvider _serviceProvider;
        private int _id;
        private readonly TaskCompletionSource<object> _initTcs = new TaskCompletionSource<object>();
        private Dictionary<string, TaskCompletionSource<JToken>> _results = new Dictionary<string, TaskCompletionSource<JToken>>();
        private readonly object _lockObj = new object();

        public PluginContainer(string pluginHost,
                               PluginDescription pluginDescriptor,
                               IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _pluginDescriptor = pluginDescriptor;
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pluginHost,
                    Arguments = Process.GetCurrentProcess().Id.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            _process.OutputDataReceived += OnOutputData;
            _process.ErrorDataReceived += OnErrorData;
            _process.Exited += OnProcessExit;
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine($"{_pluginDescriptor.Name}: exited");

            _initTcs.TrySetException(new Exception("Plugin process died"));
        }

        public Task<T> CallAsync<T>(Type type, string method, params object[] args)
        {
            return CallAsync<T>(type.AssemblyQualifiedName, method, args);
        }

        public async Task<T> CallAsync<T>(string typeName, string method, params object[] args)
        {
            var tcs = new TaskCompletionSource<JToken>();

            lock (_lockObj)
            {
                _id++;

                var callId = _id.ToString();
                var call = JsonConvert.SerializeObject(new
                {
                    id = callId,
                    type = typeName,
                    method = method,
                    args = args
                });

                _results[callId] = tcs;

                _process.StandardInput.WriteLine(call);
            }

            var result = await tcs.Task;
            return result.ToObject<T>();
        }

        public IPluginData TransformData(IPluginData data)
        {
            return CallAsync<PluginData>(typeof(IPlugin), "TransformData", data).GetAwaiter().GetResult();
        }

        private void OnErrorData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            Console.WriteLine($"{_pluginDescriptor.Name}: {e.Data}");
        }

        private void OnOutputData(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            Console.WriteLine($"{_pluginDescriptor.Name}: {e.Data}");

            var obj = JObject.Parse(e.Data);

            var message = obj.Value<string>("message");
            if (!string.IsNullOrEmpty(message))
            {
                return;
            }

            // RPC call from plugin
            var methodName = obj.Value<string>("method");
            var methodArgs = obj.Value<JArray>("args");
            var typeName = obj.Value<string>("type");

            if (!string.IsNullOrEmpty(typeName))
            {
                var serializer = _serviceProvider.GetService<JsonSerializer>();

                var type = Type.GetType(typeName);
                var instance = _serviceProvider.GetService(type);
                var methodInfo = type.GetMethod(methodName);
                var callArgs = methodInfo.GetParameters().Select((p, i) => methodArgs[i].ToObject(p.ParameterType, serializer)).ToArray();
                methodInfo.Invoke(instance, callArgs);
            }

            // Results of RPC call
            var id = obj.Value<string>("id");
            var result = obj["result"];
            var error = obj.Value<string>("error");

            if (!string.IsNullOrEmpty(id))
            {
                TaskCompletionSource<JToken> tcs;
                if (_results.TryGetValue(id, out tcs))
                {
                    if (string.IsNullOrEmpty(error))
                    {
                        tcs.TrySetResult(result);
                    }
                    else
                    {
                        tcs.TrySetException(new Exception(error));
                    }
                }
            }
        }

        public void Initialize(Dictionary<string, string> dependencies)
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // Initialize dependencies
            CallAsync<object>("PluginContainer", "InitializeDependencies", dependencies).GetAwaiter().GetResult();

            // Initialize the plugin
            CallAsync<object>("PluginContainer", "InitializePlugin", _pluginDescriptor.Name).GetAwaiter().GetResult();
        }

        public void Stop()
        {
            _process.Kill();
        }
    }
}
