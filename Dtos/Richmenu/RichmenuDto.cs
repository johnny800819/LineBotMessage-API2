using LineBotMessage.Dtos.Actions;

namespace LineBotMessage.Dtos.Richmenu
{
    public class RichMenuDto
    {
        public string? RichMenuId { get; set; }
        public Size Size { get; set; }
        public bool Selected { get; set; }
        public string Name { get; set; }
        public string ChatBarText { get; set; }
        public Area[] Areas { get; set; }
    }

    public class Size
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Area
    {
        public Bounds Bounds { get; set; }
        public ActionDto Action { get; set; }
    }

    public class Bounds
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
