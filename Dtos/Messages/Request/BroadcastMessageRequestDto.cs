namespace LineBotMessage.Dtos.Messages.Request
{
    public class BroadcastMessageRequestDto<T>
    {
        /// <summary>
        /// Messages 的型別是 List<T> 使用到了泛型，這是因為訊息的屬性非常多種的原因，在傳送時先決定好目前要傳送的訊息類型能夠比較簡單直接的實作功能。
        /// </summary>
        public List<T> Messages { get; set; }
        public bool? NotificationDisabled { get; set; }
    }
}
