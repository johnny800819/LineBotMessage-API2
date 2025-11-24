using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace LineBotMessage.Domain
{
    public class ServerCheckService
    {
        private readonly string IPGroup;
        private readonly string PortGroup;
        private readonly ILogger<ServerCheckService> _logger;
        private readonly HttpClient _httpClient;

        // Constructer
        public ServerCheckService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ServerCheckService> logger
            )
        {
            _logger = logger;
            _httpClient = httpClient;

            // 從 DI 注入的 IConfiguration 讀取
            IPGroup = configuration.GetSection("IPSettings:CheckIP").Value!;
            PortGroup = configuration.GetSection("IPSettings:CheckPort").Value!;

            if (string.IsNullOrEmpty(IPGroup))
            {
                throw new InvalidOperationException("IPSettings:CheckIP 未在 appsettings.json 中設定。");
            }
        }

        public string PingServer()
        {
            Ping pingSender = new Ping();
            StringBuilder pingMessage = new StringBuilder();

            var IPs = IPGroup.Split(';');
            foreach (var IP in IPs)
            {
                if (string.IsNullOrWhiteSpace(IP)) { break; }
                try
                {
                    PingReply reply = pingSender.Send(IP, 1000); // Timeout set to 1000 ms

                    if (reply.Status == IPStatus.Success)
                    {
                        pingMessage.Append($"{IP} - Ping 成功 \n");
                        _logger.LogInformation($"{IP} - Ping 成功");
                    }
                    else
                    {
                        pingMessage.Append($"{IP} - Ping 失敗 \n");
                        _logger.LogWarning($"{IP} - Ping 失敗");
                    }
                }
                catch (Exception ex)
                {
                    pingMessage.Append($"{IP} - Ping 失敗: {ex.Message}");
                    _logger.LogWarning($"{IP} - Ping 失敗: {ex.Message}");
                }
            }
            return pingMessage.ToString();
        }

        public async Task CheckPort()
        {
            var IPs = IPGroup.Split(';');
            var Ports = PortGroup.Split(';');
            foreach (var IP in IPs)
            {
                if (string.IsNullOrWhiteSpace(IP)) { break; }
                foreach (var Port in Ports)
                {
                    int PortNumber = Int32.Parse(Port);
                    using (TcpClient tcpClient = new TcpClient())
                    {
                        try
                        {
                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                            {
                                await tcpClient.ConnectAsync(IP, PortNumber).WaitAsync(cts.Token);
                                _logger.LogInformation($"{IP}:{PortNumber} - 連接成功");
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogWarning($"{IP}:{PortNumber} - 連接超時");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"{IP}:{PortNumber} - 連接失敗: {ex.Message}");
                        }
                        finally
                        {
                            tcpClient.Close();
                            tcpClient.Dispose();
                        }
                    }
                }
            }
        }

        public async Task<String> CheckADuserInfo(string accountOrName)
        {
            // 偵測AD使用者的密碼到期日
            var stringBuilder = new StringBuilder();
            string apiUrl = $"https://febsr20te.feb.gov.tw/API1/AD/UserPwdLastSetCheckOne?adAccount={accountOrName}";
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return responseData;
            }
            else
            {
                // 記錄詳細錯誤信息
                _logger.LogError("Request to {Url} failed with status code {StatusCode} and reason {ReasonPhrase}", apiUrl, response.StatusCode, response.ReasonPhrase);

                stringBuilder.Append(response.StatusCode.ToString());
                stringBuilder.Append("服務器內部錯誤，\n請稍後再試");

                // 返回通用錯誤信息
                return stringBuilder.ToString();
            }
        }
    }
}
