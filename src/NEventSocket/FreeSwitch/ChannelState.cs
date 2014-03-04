namespace NEventSocket.FreeSwitch
{
    public enum ChannelState
    {
        New,
        Init,
        Routing,
        SoftExecute,
        Execute,
        ExchangeMedia,
        Park,
        ConsumeMedia,
        Hibernate,
        Reset,
        Hangup,
        Done,
        Destroy,
    }
}