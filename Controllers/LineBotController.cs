using LineBotMessage.Domain;
using LineBotMessage.Dtos.Richmenu;
using LineBotMessage.Dtos.Webhook;
using LineBotMessage.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LineBotMessage.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LineBotController : ControllerBase
    {
        // 宣告Service
        private readonly LineBotService _lineBotService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<LineBotController> _logger;
        private readonly ServerCheckService _serverCheckService;
        private readonly RichMenuService _richMenuService;

        // Constructor 實例化 service class
        public LineBotController(
            // .NET DI 系統會自動從 Program.cs 尋找註冊過的服務，並"注入"到這裡
            LineBotService lineBotService,
            RichMenuService richMenuService,
            ServerCheckService serverCheckService,
            HttpClient httpClient,
            ILogger<LineBotController> logger)
        {
            // --- 接收 DI 注入的實例 ---
            _lineBotService = lineBotService;
            _richMenuService = richMenuService;
            _serverCheckService = serverCheckService;

            _httpClient = httpClient;
            _logger = logger;
        }

        // 使用 Post 方法：接收 Line 傳送的 Webhook Event
        [HttpPost("Webhook")]
        public async Task<IActionResult> Webhook(WebhookRequestBodyDto body)
        {
            await _lineBotService.ReceiveWebhook(body); // 呼叫 Service
            return Ok();
        }

        // 使用 {"Messages":[{"Type":"text","Text":"廣播測試"}]} 這樣的JSON訊息格式傳送
        [HttpPost("SendMessage/Broadcast")]
        public async Task<IActionResult> Broadcast([Required] string messageType, object body)
        {
            await _lineBotService.BroadcastMessageHandler(messageType, body);
            return Ok();
        }

        // ============== Rich Menu API ==============
        [HttpPost("RichMenu/Validate")]
        public async Task<IActionResult> ValidateRichMenu(RichMenuDto richMenu)
        {
            return Ok(await _richMenuService.ValidateRichMenu(richMenu));
        }

        [HttpPost("RichMenu/Create")]
        public async Task<IActionResult> CreateRichMenu(RichMenuDto richMenu)
        {
            return Ok(await _richMenuService.CreateRichMenu(richMenu));
        }

        [HttpGet("RichMenu/GetList")]
        public async Task<IActionResult> GetRichMenuList()
        {
            return Ok(await _richMenuService.GetRichMenuList());
        }

        [HttpPost("RichMenu/UploadImage/{richMenuId}")]
        public async Task<IActionResult> UploadRichMenuImage(IFormFile imageFile, string richMenuId)
        {
            return Ok(await _richMenuService.UploadRichMenuImage(richMenuId, imageFile));
        }

        [HttpGet("RichMenu/SetDefault/{richMenuId}")]
        public async Task<IActionResult> SetDefaultRichMenu(string richMenuId)
        {
            return Ok(await _richMenuService.SetDefaultRichMenu(richMenuId));
        }
    }
}