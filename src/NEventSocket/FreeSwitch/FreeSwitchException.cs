namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class FreeSwitchException : Exception
    {
        public FreeSwitchException()
        {
        }

        public FreeSwitchException(string message)
            : base(message)
        {
        }

        public FreeSwitchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected FreeSwitchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}