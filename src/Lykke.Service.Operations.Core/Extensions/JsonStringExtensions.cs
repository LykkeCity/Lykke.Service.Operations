using System;
using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Operations.Core.Extensions
{
    public static class JsonStringExtensions
    {
        private const string EMPTY_JSON = "{}";

        public static JToken Empty()
        {
            return JToken.FromObject(EMPTY_JSON);
        }

        public static string ToJsonString(this object @object, bool indent = false)
        {
            if (@object is Delegate) return "{}";
            return JsonConvert.SerializeObject(@object, indent ? Formatting.Indented : Formatting.None);
        }

        public static string ToJsonString(this string content, bool indent = false)
        {
            return EMPTY_JSON.Merge(content, indent);
        }

        public static bool IsEmptyContent(this string content)
        {
            return string.IsNullOrEmpty(content) || content == EMPTY_JSON;
        }

        public static JContainer ToJContainer(this object @object)
        {
            if (@object is IList || @object is ICollection)
            {
                return JArray.FromObject(@object);
            }
            return JObject.FromObject(@object);
        }

        public static JObject ToJObject(this object @object)
        {
            return JObject.FromObject(@object);
        }

        public static JObject Merge(this JObject parentContent, JObject childContent)
        {
            merge(parentContent, childContent);
            return parentContent;
        }

        public static string Merge(this string parentContent, string childContent, bool indent = false)
        {
            JObject result = JObject.Parse(parentContent);
            merge(result, JObject.Parse(childContent));
            return result.ToString(indent ? Formatting.Indented : Formatting.None);
        }

        public static string Merge(this string parentContent, object childContent, bool indent = false)
        {
            JObject result = JObject.Parse(parentContent);
            merge(result, JObject.FromObject(childContent));
            return result.ToString(indent ? Formatting.Indented : Formatting.None);
        }

        private static void merge(JObject receiver, JObject donor)
        {
            foreach (var property in donor)
            {
                var receiverValue = receiver[property.Key] as JObject;
                var donorValue = property.Value as JObject;
                if (receiverValue != null && donorValue != null)
                {
                    merge(receiverValue, donorValue);
                }
                else
                    receiver[property.Key] = property.Value;
            }
        }

        public static string Diff(this string parentContent, string childContent, bool indent = false)
        {
            var result = Diff(JObject.Parse(parentContent), JObject.Parse(childContent));
            return result.ToString(indent ? Formatting.Indented : Formatting.None);
        }

        public static JObject Diff(this JObject original, JObject changed)
        {
            return diff(original, changed);
        }

        private static JObject diff(JObject original, JObject changed)
        {
            var result = new JObject();
            foreach (var property in changed)
            {
                var originalValue = original[property.Key] as JObject;
                var changedValue = property.Value as JObject;
                if (originalValue != null && changedValue != null)
                {
                    var val = Diff(originalValue, changedValue);
                    if (val.HasValues)
                        result[property.Key] = val;
                }
                else if (!property.Value.Equals(original[property.Key]))
                    result[property.Key] = property.Value;
            }

            foreach (var property in original)
            {
                if (changed[property.Key] == null)
                {
                    result[property.Key] = null;
                }
            }
            return result;
        }

        public static T JsonValue<T>(this string content, string path)
        {
            JObject result;
            try
            {
                result = JObject.Parse(content);
            }
            catch (JsonReaderException)
            {
                return default(T);
            }

            var selectToken = result.SelectToken(path);
            return selectToken != null ? selectToken.Value<T>() : default(T);
        }

        public static T JsonObject<T>(this string content, string path)
        {
            return JsonObject<T>(JObject.Parse(content), path);
        }

        public static T JsonObject<T>(this JObject jObject, string path)
        {
            JToken selectToken = jObject.SelectToken(path);
            return selectToken != null ? selectToken.ToObject<T>() : default(T);
        }

        public static T JsonObject<T>(this string content)
        {
            JObject result = JObject.Parse(content);
            return result.Root.ToObject<T>();
        }

        public static object JsonObject(this string content, Type type)
        {
            JObject result = JObject.Parse(content);
            return result.Root.ToObject(type);
        }

        public static JObject ToJsonObject(this string content)
        {
            JObject result = JObject.Parse(content);
            return result;
        }

        /// <summary>
        /// получает значение из jobject по пути path
        /// внутри определен массив delimeters, который возвращается как есть
        /// если path не найден не throwErrorIfPathNotFinded тогда возвращается path
        /// </summary>
        /// <param name="jobject"></param>
        /// <param name="path"></param>
        /// <param name="throwErrorIfPathNotFinded"></param>
        /// <returns></returns>
        public static string GetStringValue(this JObject jobject, string path, bool throwErrorIfPathNotFinded = true)
        {
            var delimiters = new string[] { " " };
            if (delimiters.Contains(path)) return path;
            JToken token = jobject;
            try
            {
                path.Split('.').ToList().ForEach(key =>
                {
                    token = token[key];
                    if (token == null)
                    {
                        throw new InvalidOperationException(
                            string.Format("Part \"{0}\" in path \"{1}\" does not route to a property.", key, path));
                    }
                });
            }
            catch (InvalidOperationException)
            {
                if (throwErrorIfPathNotFinded) throw;
                else return path;
            }
            return token.Value<string>();
        }

        /// <summary>
        /// получает значение из jobject(в stringe) по пути path
        /// внутри определен массив delimeters, который возвращается как есть
        /// если path не найден не throwErrorIfPathNotFinded тогда возвращается path
        /// </summary>
        /// <param name="jobject"></param>
        /// <param name="path"></param>
        /// <param name="throwErrorIfPathNotFinded"></param>
        /// <returns></returns>
        public static string GetStringValue(this string jobject, string path, bool throwErrorIfPathNotFinded = true)
        {
            return GetStringValue(JObject.Parse(jobject), path, throwErrorIfPathNotFinded);
        }
    }
}
