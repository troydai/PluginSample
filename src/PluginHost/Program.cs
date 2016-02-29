using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace OutOfProcessPluginContainer
{
    public class HostContext
    {
        public int HostPID { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // hostpid
            var context = new HostContext();
            context.HostPID = Int32.Parse(args[0]);

            var serializer = new JsonSerializer();
            serializer.Converters.Add(new PluginDataConverter());

            //Debugger.Launch();

            var services = new ServiceCollection();
            services.AddInstance(context);
            services.AddSingleton<MessageWriter>();
            services.AddSingleton<PluginContainer>();
            services.AddInstance(serializer);

            var sp = services.BuildServiceProvider();

            var manager = sp.GetService<PluginContainer>();
            manager.Initialize();
        }
    }
}
