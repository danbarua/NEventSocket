namespace NEventSocket.FreeSwitch.Api
{
    /// <summary>
    /// Represents options that may be used with the Originate api command
    /// </summary>
    /// <remarks>
    /// https://wiki.freeswitch.org/wiki/Channel_Variables#Originate_related_variables
    /// </remarks>
    public class OriginateOptions
    {
        public string UUID { get; set; }
        public string CallerIdName { get; set; }
        public string CallerIdNumber { get; set; }
        public string GroupConfirmKey { get; set; }
        public string GroupConfirmFile { get; set; }
        public bool ForkedDial { get; set; }
        public bool IgnoreEarlyMedia { get; set; }
        public int Retries { get; set; }
        public int RetrySleepMs { get; set; }
        public int Timeout { get; set; }
        public bool SipAutoAnswer { get; set; }
        public string ExecuteOnOriginate { get; set; }

        /// <summary>
        /// http://blog.godson.in/2010/12/use-of-returnringready-originate.html
        /// </summary>
        public bool ReturnOnRingReady { get; set; }
    }
}