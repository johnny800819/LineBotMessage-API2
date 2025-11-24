namespace LineBotMessage.Dtos.Actions
{
    public class ActionDto
    {
        public string Type { get; set; }
        public string? Label { get; set; }

        //Postback action.
        public string? Data { get; set; }
        public string? DisplayText { get; set; }
        public string? InputOption { get; set; }
        public string? FillInText { get; set; }

        //Message action.
        public string? Text { get; set; }

        //Uri action.
        public string? Uri { get; set; }
        public UriActionAltUriDto? AltUri { get; set; }

        // datetime picker action
        public string? Mode { get; set; }
        public string? Initial { get; set; }
        public string? Max { get; set; }
        public string? Min { get; set; }
    }

    public class UriActionAltUriDto
    {
        public string Desktop { get; set; }
    }
}
