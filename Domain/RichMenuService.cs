using LineBotMessage.Dtos.Richmenu;
using LineBotMessage.Providers;
using System.Net.Http.Headers;
using System.Text;

namespace LineBotMessage.Domain
{
    public class RichMenuService
    {
        private readonly ILogger<RichMenuService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _channelAccessToken;

        private readonly JsonProvider _jsonProvider = new JsonProvider();

        private readonly string validateRichMenuUri = "https://api.line.me/v2/bot/richmenu/validate";
        private readonly string createRichMenuUri = "https://api.line.me/v2/bot/richmenu";
        private readonly string getRichMenuListUri = "https://api.line.me/v2/bot/richmenu/list";

        // {0} 的位置要帶入 richMenuId
        private readonly string uploadRichMenuImageUri = "https://api-data.line.me/v2/bot/richmenu/{0}/content";
        // {0} 的位置要帶入 richMenuId
        private readonly string setDefaultRichMenuUri = "https://api.line.me/v2/bot/user/all/richmenu/{0}";

        public RichMenuService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<RichMenuService> logger
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _channelAccessToken = configuration["LineBotSettings:ChannelAccessToken"]!;

            if (string.IsNullOrEmpty(_channelAccessToken))
            {
                throw new InvalidOperationException("LineBotSettings:ChannelAccessToken 金鑰未設定。");
            }
        }

        // 將傳入的 rich menu 物件送到 Line 去驗證其格式是否正確。
        public async Task<string> ValidateRichMenu(RichMenuDto richMenu)
        {
            var jsonBody = new StringContent(_jsonProvider.Serialize(richMenu), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(validateRichMenuUri),
                Content = jsonBody,
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);
            var response = await _httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }

        // 將傳入的格式送到 Line 然後建立 rich menu，並且其格式內容會儲存在 Line 平台中，一支 Line Bot 最多可以儲存 1000 張 rich menu，建立成功後會收到建立好的 richmenuId。
        public async Task<string> CreateRichMenu(RichMenuDto richMenu)
        {
            var jsonBody = new StringContent(_jsonProvider.Serialize(richMenu), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(createRichMenuUri),
                Content = jsonBody,
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);
            var response = await _httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }

        // 回傳目前儲存在 Line 上的所有 richmenu 格式。
        public async Task<RichMenuListDto> GetRichMenuList()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(getRichMenuListUri),
            };
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);
            var response = await _httpClient.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("GetRichMenuList response LogInformation : {ResponseBody}", responseBody);

            var list = _jsonProvider.Deserialize<RichMenuListDto>(responseBody);

            // 依照名稱排序
            list.Richmenus = list.Richmenus.OrderBy((rm) => rm.Name).ToList();
            return list;
        }

        // 這個 function 是使用剛剛 create rich menu 成功收到的 rich menu id 去上傳圖片，這裡傳送訊息時圖片是採用 URL 的方式不同，rich menu 上傳圖片是將整格檔案內容傳給 Line
        public async Task<string> UploadRichMenuImage(string richMenuId, IFormFile imageFile)
        {
            //判斷檔案格式 需為 png or jpeg
            if (!(Path.GetExtension(imageFile.FileName).Equals(".png", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(imageFile.FileName).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
            {
                return "圖片格式錯誤，須為 png or jpeg";
            }
            using (var stream = new MemoryStream())
            {
                //建立檔案內容
                imageFile.CopyTo(stream);
                var fileBytes = stream.ToArray();
                var content = new ByteArrayContent(fileBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                var request = new HttpRequestMessage(HttpMethod.Post, String.Format(uploadRichMenuImageUri, richMenuId))
                {
                    Content = content
                };
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);
                var response = await _httpClient.SendAsync(request);

                return await response.Content.ReadAsStringAsync();
            }
        }

        // 將指定的 rich menu 設為預設顯示的 rich menu，沒有被特別設定的使用者就會看到預設的 rich menu。
        public async Task<string> SetDefaultRichMenu(string richMenuId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, String.Format(setDefaultRichMenuUri, richMenuId));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken);

            var response = await _httpClient.SendAsync(request);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
