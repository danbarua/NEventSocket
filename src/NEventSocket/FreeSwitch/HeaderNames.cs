namespace NEventSocket.FreeSwitch
{
    public static class HeaderNames
    {
        public const string ContentLength = "Content-Length";

        public const string ContentType = "Content-Type";

        public const string CallerUniqueId = "Caller-Unique-ID";

        /// <summary>
        /// In a CHANNEL_EXECUTE_COMPLETE, contains the application that was executed
        /// </summary>
        public const string Application = "Application";

        /// <summary>
        /// In a CHANNEL_EXECUTE_COMPLETE event, contains the args passed to the application
        /// </summary>
        public const string ApplicationData = "Application-Data";

        /// <summary>
        /// In a CHANNEL_EXECUTE_COMPLETE event, contains the response from the application
        /// </summary>
        public const string ApplicationResponse = "Application-Response";
        
        public const string EventName = "Event-Name";

        public const string ChannelState = "Channel-State";

        public const string AnswerState = "Answer-State";

        public const string HangupCause = "Hangup-Cause";

        public const string EventSubclass = "Event-Subclass";
    }
}