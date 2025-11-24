namespace LineBotMessage.Dtos.Webhook
{
    public class WebhookRequestBodyDto
    {
        public string? Destination { get; set; }
        public List<WebhookEventDto> Events { get; set; }
    }
}
