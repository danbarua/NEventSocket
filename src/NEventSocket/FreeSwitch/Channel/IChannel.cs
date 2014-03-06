namespace NEventSocket.FreeSwitch.Channel
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;
    
    public interface IChannel : IDisposable
    {
        string UUID { get; }

        ChannelState ChannelState { get; }

        AnswerState AnswerState { get; }

        HangupCause? HangupCause { get; }

        Action<EventMessage> HangupCallBack { set; }

        string GetChannelVariable(string variableName);

        Task SetChannelVariable(string variableName, string value);

        Task<Channel> Bridge(string destination, BridgeOptions options = null);

        Task Hold();

        Task Park();

        Task Hangup(HangupCause hangupCause);

        Task PlayFile(string file, Leg leg = Leg.Both, string terminator = null);

        Task<string> PlayGetDigits(string file, int numDigits, int timeout);

        Task StartRecording(string file, int maxSeconds = 0);

        Task MaskRecording();

        Task UnmaskRecording();

        Task StopRecording();
    }
}