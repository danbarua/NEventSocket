namespace NEventSocket.FreeSwitch
{
    public static class ContentTypes
    {
        public const string EventPlain = "text/event-plain";
        public const string EventJson = "text/event-json";
        public const string EventXml = "text/event-xml";
        public const string AuthRequest = "auth/request";
        public const string CommandReply = "command/reply";
        public const string ApiResponse = "api/response";
        public const string DisconnectNotice = "text/disconnect-notice";
        public const string RudeRejection = "text/rude-rejection";
    }
}