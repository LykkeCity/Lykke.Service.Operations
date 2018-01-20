using System;
using Newtonsoft.Json;

namespace Lykke.Service.Operations.Core.Extensions
{
    public static class JsonStringExtensions
    {
        public static string ToJsonString(this object @object, bool indent = false)
        {
            if (@object is Delegate) return "{}";
            return JsonConvert.SerializeObject(@object, indent ? Formatting.Indented : Formatting.None);
        }
    }
}
