using System.Net.Http.Headers;
using System.Text;
using LineBotMessage.Dtos.Messages.Request;
using LineBotMessage.Dtos.Messages;
using LineBotMessage.Dtos.Webhook;
using LineBotMessage.Enum;
using LineBotMessage.Providers;
using LineBotMessage.Domain;

namespace LineBotMessage.Services
{
    public class LineBotService
    {
        // 宣告變數
        private readonly string replyMessageUri = "https://api.line.me/v2/bot/message/reply";
        private readonly string broadcastMessageUri = "https://api.line.me/v2/bot/message/broadcast";

        private readonly ILogger<LineBotService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _channelAccessToken;
        private readonly string _channelSecret;

        private readonly JsonProvider _jsonProvider = new JsonProvider();
        private readonly ServerCheckService _serverCheckService;

        public LineBotService(
            HttpClient httpClient, // 接收 DI 傳入的 HttpClient
            IConfiguration configuration, // 接收 DI 傳入的組態 (用來讀金鑰)
            ServerCheckService serverCheckService, // 接收 DI 傳入的 ServerCheckService
            ILogger<LineBotService> logger
            )
        {
            _logger = logger;
            _httpClient = httpClient;
            _serverCheckService = serverCheckService;

            // 從 IConfiguration 安全地讀取金鑰 (來自 secrets.json)
            _channelAccessToken = configuration["LineBotSettings:ChannelAccessToken"]!;
            _channelSecret = configuration["LineBotSettings:ChannelSecret"]!;

            if (string.IsNullOrEmpty(_channelAccessToken) || string.IsNullOrEmpty(_channelSecret))
            {
                throw new InvalidOperationException("LineBotSettings 金鑰未在 secrets.json 或環境變數中設定。");
            }
        }

        public async Task ReceiveWebhook(WebhookRequestBodyDto requestBody)
        {
            foreach (var eventObject in requestBody.Events)
            {
                switch (eventObject.Type)
                {
                    case WebhookEventTypeEnum.Message:
                        string requestText = requestBody.Events.FirstOrDefault()?.Message?.Text ?? string.Empty;
                        string pingMessage = _serverCheckService.PingServer();
                        string resultMessage = "";
                        switch (requestText)
                        {
                            case "測試":
                                resultMessage = $"" +
                                                $"您好，您傳送了\"{eventObject.Message?.Text}\"! \n" +
                                                $"偵測關鍵字：{requestText}，通過\n" +
                                                $"檢測結果如下：\n{pingMessage}";
                                break;
                            default:
                                string userMessage = await _serverCheckService.CheckADuserInfo(requestText);
                                resultMessage = $"" +
                                                $"您好，您傳送了\"{eventObject.Message?.Text}\"! \n" +
                                                $"偵測關鍵字：{requestText}\n" +
                                                $"檢測結果如下：\n{userMessage}";
                                break;
                        }
                        var replyMessage = new ReplyMessageRequestDto<TextMessageDto>()
                        {
                            ReplyToken = eventObject.ReplyToken!,
                            Messages = new List<TextMessageDto>
                            {
                                new TextMessageDto(){Text = resultMessage }
                            }
                        };
                        await ReplyMessageHandler("text", replyMessage);
                        _logger.LogInformation("收到使用者傳送訊息！");
                        break;
                    case WebhookEventTypeEnum.Unsend:
                        _logger.LogInformation($"使用者{eventObject.Source.UserId}在聊天室收回訊息！");
                        break;
                    case WebhookEventTypeEnum.Follow:
                        _logger.LogInformation($"使用者{eventObject.Source.UserId}將我們新增為好友！");
                        break;
                    case WebhookEventTypeEnum.Unfollow:
                        _logger.LogInformation($"使用者{eventObject.Source.UserId}封鎖了我們！");
                        break;
                    case WebhookEventTypeEnum.Join:
                        _logger.LogInformation("我們被邀請進入聊天室了！");
                        break;
                    case WebhookEventTypeEnum.Leave:
                        _logger.LogInformation("我們被聊天室踢出了");
                        break;
                }
            }
        }

        /// <summary>
        /// 接收到廣播請求時，在將請求傳至 LINE 前多一層處理，依據收到的 messageType 將 messages 轉換成正確的型別，這樣 Json 轉換時才能正確轉換。
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="requestBody"></param>
        public async Task BroadcastMessageHandler(string messageType, object requestBody)
        {
            string? strBody = requestBody?.ToString(); // 使用 ?. 安全地 ToString()，並檢查 null 或 empty
            if (string.IsNullOrEmpty(strBody))
            {
                return;
            }

            switch (messageType)
            {
                case MessageTypeEnum.Text:
                    // strBody 在這裡保證不為 null (因為上面的 if 檢查)
                    var messageRequest = _jsonProvider.Deserialize<BroadcastMessageRequestDto<TextMessageDto>>(strBody);
                    
                    await BroadcastMessage(messageRequest);
                    break;
            }
        }

        /// <summary>
        /// 將廣播訊息請求送到 LINE
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        public async Task BroadcastMessage<T>(BroadcastMessageRequestDto<T> request)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear(); // 每次呼叫前清理
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken); // 使用讀取到的金鑰

            var json = _jsonProvider.Serialize(request);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(broadcastMessageUri),
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("BroadcastMessage 成功, Response: {ResponseBody}", responseBody);
            }
            else
            {
                _logger.LogError("BroadcastMessage 失敗, Response: {ResponseBody}", responseBody);
            }

            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// 接收到回覆請求時，在將請求傳至 LINE 前多一層處理(目前為預留)
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="requestBody"></param>
        public Task ReplyMessageHandler<T>(string messageType, ReplyMessageRequestDto<T> requestBody)
        {
            return ReplyMessage(requestBody);
        }

        /// <summary>
        /// 將回覆訊息請求送到 LINE
        /// 參考網址：https://medium.com/appxtech/day-7-%E8%AE%93-c-%E4%B9%9F%E5%8F%AF%E4%BB%A5%E5%BE%88-social-net-6-c-%E8%88%87-line-services-api-%E9%96%8B%E7%99%BC-%E5%9B%9E%E8%A6%86%E8%88%87%E5%BB%A3%E6%92%AD%E8%A8%8A%E6%81%AF-14b186697f34
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>    
        public async Task ReplyMessage<T>(ReplyMessageRequestDto<T> request)
        {
            _httpClient.DefaultRequestHeaders.Accept.Clear(); // 每次呼叫前清理
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _channelAccessToken); // 使用讀取到的金鑰

            var json = _jsonProvider.Serialize(request);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(replyMessageUri),
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("ReplyMessage 成功, Response: {ResponseBody}", responseBody);
            }
            else
            {
                _logger.LogError("ReplyMessage 失敗, Response: {ResponseBody}", responseBody);
            }
        }
    }
}