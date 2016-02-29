using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Plugin.Abstractions;

namespace Plugin1
{
    public class SpanishGreeter : IPlugin
    {
        private readonly ILogger _logger;

        public IPluginData TransformData(IPluginData data)
        {
            _logger?.LogInformation("Logging stuff");
            return new PluginData { Message = $"Hola {data.Message} - {JsonConvert.SerializeObject(new { X = 1 })}" };
        }

        private class PluginData : IPluginData
        {
            public string Message { get; set; }
        }
    }
}
