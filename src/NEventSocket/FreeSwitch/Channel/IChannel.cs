namespace NEventSocket.FreeSwitch.Channel
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;

    public interface IChannel
    {
        string UUID { get; }

        ChannelState ChannelState { get; }

        string AnswerState { get; }

        Task<OriginateResult> Bridge(IEndpoint destination, BridgeOptions options = null);

        Task Hold();

        Task Park();

        Task Hangup();

        Task PlayFile(string file, Leg leg = Leg.Both, string terminator = null);

        Task<string> PlayGetDigits(string file, int numDigits, int timeout);

        Task StartRecording(string file, int maxSeconds = 0);

        Task MaskRecording();

        Task UnmaskRecording();

        Task StopRecording();

        IObservable<string> DTMF { get; }
    }
}