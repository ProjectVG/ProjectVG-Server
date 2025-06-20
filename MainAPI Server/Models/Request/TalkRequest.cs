
namespace MainAPI_Server.Models.Request
{
    public class TalkRequest
    {
        public string Id { get; set; }
        public string Actor { get; set; }
        public string Message { get; set; }
        public string? Action { get; set; }
    }

}
