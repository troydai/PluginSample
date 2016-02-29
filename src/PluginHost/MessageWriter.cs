using System;
using System.IO;
using Newtonsoft.Json;

namespace OutOfProcessPluginContainer
{
    public class MessageWriter
    {
        private readonly object _lockObj = new object();
        private readonly JsonSerializer _serializer;

        public MessageWriter(JsonSerializer serializer)
        {
            _serializer = serializer;
        }

        public void WriteMessage(string message)
        {
            Write(new { message = message });
        }

        public void Write(object data)
        {
            try
            {
                lock (_lockObj)
                {
                    using (var sw = new StringWriter())
                    {
                        _serializer.Serialize(sw, data);
                        Console.WriteLine(sw.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }

}
