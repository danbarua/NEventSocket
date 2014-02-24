namespace NEventSocket.FreeSwitch
{
    using System.Collections.Generic;

    using NEventSocket.Util;

    public class ChannelVariable
    {
        public ChannelVariable(string name, object value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; private set; }

        public object Value { get; private set; }

        public override string ToString()
        {
            return "{0}={1}".Fmt(Name, Value);
        }
    }
}