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

        string this[string variableName] { get; set; }

        Task<BridgeResult> Bridge(string destination, BridgeOptions options, Action<EventMessage> onProgress = null);

        Task Hold();

        Task Park();

        Task RingReady();

        Task Answer();

        Task Hangup(HangupCause hangupCause);

        Task Sleep(int milliseconds);

        Task PlayFile(string file, Leg leg = Leg.Both, string terminator = null);

        Task<string> PlayGetDigits(PlayGetDigitsOptions options);

        Task StartRecording(string file, int maxSeconds = 0);

        Task MaskRecording();

        Task UnmaskRecording();

        Task StopRecording();
    }
}