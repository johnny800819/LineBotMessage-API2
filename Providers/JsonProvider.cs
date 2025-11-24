using System.Text.Json;
using System.Text.Json.Serialization;

namespace LineBotMessage.Providers
{
    /// <summary>
    /// 因為 Line 接收格式的變數名稱開頭為小寫，但 C# 習慣的命名規則 Class 變數名稱為大寫開頭，所以在傳送 Json 給 Line 的過程中無法直接使用 JsonSerializer 做資料的處理(序列化/反序列化)，所以我們需要加工一下，自己包一個 Provider 來處理 Json 轉換的問題
    /// </summary>
    public class JsonProvider
    {
        private JsonSerializerOptions serializeOption = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static JsonSerializerOptions deserializeOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        public string Serialize<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, serializeOption);
        }

        public T Deserialize<T>(string str)
        {
            return JsonSerializer.Deserialize<T>(str, deserializeOptions);
        }
    }
}