using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RosSharp.RosBridgeClient
{
    /// <summary>
    /// 处理 ROS2 消息中 null 值的 JsonConverter
    /// ROS2 控制器在没有数据时发布 null（如 effort: [null, null, ...]），
    /// 但 C# 值类型数组（double[], float[], int[]）不能接受 null。
    /// 此 Converter 将 null 元素安全转换为类型默认值（0）。
    /// </summary>
    public class NullToDefaultConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsArray;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var elementType = objectType.GetElementType();

            if (token.Type == JTokenType.Null)
                return null;

            if (token.Type != JTokenType.Array)
                return token.ToObject(objectType, serializer);

            var jArray = (JArray)token;
            var result = Array.CreateInstance(elementType, jArray.Count);

            for (int i = 0; i < jArray.Count; i++)
            {
                var element = jArray[i];
                if (element.Type == JTokenType.Null)
                {
                    result.SetValue(GetDefaultValue(elementType), i);
                }
                else
                {
                    try
                    {
                        result.SetValue(element.ToObject(elementType, serializer), i);
                    }
                    catch
                    {
                        result.SetValue(GetDefaultValue(elementType), i);
                    }
                }
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(double)) return 0.0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(int)) return 0;
            if (type == typeof(long)) return 0L;
            if (type == typeof(bool)) return false;
            return null;
        }
    }
}
