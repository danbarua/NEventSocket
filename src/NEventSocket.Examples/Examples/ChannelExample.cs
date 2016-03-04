namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    public class ChannelExample : ICommandLineTask, IDisposable
    {
        private readonly CommandLineReader commandLineReader;

        private OutboundListener listener;

        public ChannelExample(CommandLineReader commandLineReader)
        {
            this.commandLineReader = commandLineReader;
        }

        public Task Run(CancellationToken cancellationToken)
        {
            listener = new OutboundListener(8084);

            listener.Channels.Subscribe(
                async channel =>
                {
                    try
                    {
                        channel.HangupCallBack = (e) =>
                        {
                            ColorConsole.WriteLine(
                                "Hangup Detected on A-Leg {0} {1}".Fmt(
                                    e.Headers[HeaderNames.CallerUniqueId],
                                    e.Headers[HeaderNames.HangupCause]).Red());
                            ColorConsole.WriteLine("Aleg bridge {0}".Fmt(channel.Bridge.HangupCause).OnRed());

                            ColorConsole.WriteLine(e.ToString().DarkGreen());
                        };

                        await channel.Answer();

                        var bridgeOptions = new BridgeOptions()
                                            {
                                                UUID = Guid.NewGuid().ToString(),
                                                IgnoreEarlyMedia = true,
                                                RingBack = "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1",
                                                ContinueOnFail = true,
                                                HangupAfterBridge = true,
                                                TimeoutSeconds = 60,
                                                CallerIdName = channel.Advanced.GetVariable("effective_caller_id_name"),
                                                CallerIdNumber =
                                                    channel.Advanced.GetVariable("effective_caller_id_number"),
                                            };

                        bridgeOptions.ChannelVariables.Add("bridge_filter_dtmf", "true");

                        await channel.BridgeTo("user/1003", bridgeOptions);
                            //, (e) => ColorConsole.WriteLine("Bridge Progress Ringing...".DarkGreen()));

                        if (!channel.Bridge.IsBridged)
                        {
                            ColorConsole.WriteLine("Bridge Failed - {0}".Fmt(channel.Bridge.HangupCause).Red());
                            await channel.PlayFile("ivr/8000/ivr-call_rejected.wav");
                            await channel.Hangup(HangupCause.NormalTemporaryFailure);
                        }
                        else
                        {
                            ColorConsole.WriteLine("Bridge success - {0}".Fmt(channel.Bridge.ResponseText).DarkGreen());

                            channel.Bridge.Channel.HangupCallBack =
                                (e) =>
                                    ColorConsole.WriteLine(
                                        "Hangup Detected on B-Leg {0} {1}".Fmt(
                                            e.Headers[HeaderNames.CallerUniqueId],
                                            e.Headers[HeaderNames.HangupCause]).Red());

                            ColorConsole.WriteLine("Enabling feature codes on the B-Leg: ".DarkGreen());
                            ColorConsole.WriteLine("Press ".DarkGreen(), "#7".Yellow(), " to Start Recording".DarkGreen());
                            ColorConsole.WriteLine("Press ".DarkGreen(), "#8".Yellow(), " to Stop Recording".DarkGreen());
                            ColorConsole.WriteLine("Press ".DarkGreen(), "#4".Yellow(), " to Pause Recording".DarkGreen());
                            ColorConsole.WriteLine("Press ".DarkGreen(), "#5".Yellow(), " to Resume Recording".DarkGreen());
                            ColorConsole.WriteLine("Press ".DarkGreen(), "#9".Yellow(), " for attended transfer".DarkGreen());

                            await channel.SetChannelVariable("RECORD_STEREO", "true");
                            var recordingPath = "{0}.wav".Fmt(channel.UUID);

                            channel.Bridge.Channel.FeatureCodes("#").Subscribe(
                                async x =>
                                {
                                    try
                                    {
                                        ColorConsole.WriteLine("Detected Feature Code: ".DarkYellow(), x);
                                        switch (x)
                                        {
                                            case "#4":
                                                ColorConsole.WriteLine("Mask recording".Yellow());
                                                await channel.MaskRecording();
                                                await channel.PlayFile("ivr/8000/ivr-recording_paused.wav", Leg.BLeg);
                                                break;
                                            case "#5":
                                                ColorConsole.WriteLine("Unmask recording".Yellow());
                                                await channel.UnmaskRecording();
                                                await channel.PlayFile("ivr/8000/ivr-begin_recording.wav", Leg.BLeg);
                                                break;
                                            case "#8":
                                                ColorConsole.WriteLine("Stop recording".Yellow());
                                                await channel.StopRecording();
                                                await channel.PlayFile("ivr/8000/ivr-recording_stopped.wav", Leg.Both);
                                                break;
                                            case "#7":
                                                ColorConsole.WriteLine("Start recording".Yellow());
                                                await channel.StartRecording(recordingPath);
                                                await channel.PlayFile("ivr/8000/ivr-begin_recording.wav", Leg.Both);
                                                break;
                                            case "#9":
                                                ColorConsole.WriteLine("Attended x-fer".Yellow());
                                                await
                                                    Task.WhenAll(
                                                        channel.PlayFile("ivr/8000/ivr-call_being_transferred.wav"),
                                                        channel.Bridge.Channel.PlayFile("misc/8000/transfer1.wav"));

                                                var digits =
                                                    await
                                                        channel.Bridge.Channel.Read(
                                                            new ReadOptions
                                                            {
                                                                MinDigits = 3,
                                                                MaxDigits = 4,
                                                                Prompt = "tone_stream://%(10000,0,350,440)",
                                                                TimeoutMs = 30000,
                                                                Terminators = "#"
                                                            });
                                                if (digits.Result == ReadResultStatus.Success && digits.Digits.Length == 4)
                                                {
                                                    await channel.Bridge.Channel.SetChannelVariable("recording_follow_attxfer", "true");
                                                    await channel.Bridge.Channel.SetChannelVariable("origination_cancel_key", "#");
                                                    await
                                                        channel.Bridge.Channel.SetChannelVariable(
                                                            "transfer_ringback",
                                                            "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1");

                                                    await
                                                        channel.Bridge.Channel.PlayFile(
                                                            "ivr/8000/ivr-please_hold_while_party_contacted.wav");

                                                    //todo: push this logic into the channel itself?

                                                    channel.ExitOnHangup = false;
                                                        //we might want to notify b+c parties if the transfer failed
                                                    var xfer = await channel.Bridge.Channel.AttendedTransfer("user/{0}".Fmt(digits));
                                                    channel.ExitOnHangup = true; //re enable exit on hangup

                                                    ColorConsole.WriteLine("XFER: {0} {1}".Fmt(xfer.Status, xfer.HangupCause).Yellow());

                                                    if (xfer.Status != AttendedTransferResultStatus.Failed)
                                                    {
                                                        await channel.PlayFile("misc/8000/transfer2.wav", Leg.Both);
                                                    }
                                                    else
                                                    {
                                                        if (!channel.IsAnswered && channel.Bridge.Channel.IsAnswered)
                                                        {
                                                            await
                                                                channel.Bridge.Channel.PlayFile(
                                                                    "ivr/8000/ivr-call_attempt_aborted.wav",
                                                                    Leg.Both);

                                                            //as a-leg has disconnected, we'll close the socket when b-leg hangs up
                                                            //todo: what if it's a three-way?!
                                                            channel.Bridge.Channel.HangupCallBack = async _ => await channel.Exit();

                                                            return;
                                                        }

                                                        if (xfer.HangupCause == HangupCause.CallRejected)
                                                        {
                                                            await channel.Bridge.Channel.PlayFile("ivr/8000/ivr-call_rejected.wav");
                                                        }
                                                        else if (xfer.HangupCause == HangupCause.NoUserResponse
                                                                 || xfer.HangupCause == HangupCause.NoAnswer)
                                                        {
                                                            await channel.Bridge.Channel.PlayFile("ivr/8000/ivr-no_user_response.wav");
                                                        }
                                                        else if (xfer.HangupCause == HangupCause.UserBusy)
                                                        {
                                                            await channel.Bridge.Channel.PlayFile("ivr/8000/ivr-user_busy.wav");
                                                        }
                                                        else
                                                        {
                                                            await
                                                                channel.Bridge.Channel.PlayFile(
                                                                    "ivr/8000/ivr-call_cannot_be_completed_as_dialed.wav");
                                                        }
                                                    }
                                                }

                                                break;
                                        }
                                    }
                                    catch (OperationCanceledException ex)
                                    {
                                        ColorConsole.WriteLine("TaskCancelled - shutting down\r\n{0}".Fmt(ex.ToString()).OnRed());
                                        ColorConsole.WriteLine("Channel {0} is {1}".Fmt(channel.UUID, channel.Answered).OnRed());
                                    }
                                });
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        ColorConsole.WriteLine("TaskCancelled - shutting down\r\n{0}".Fmt(ex.ToString()).OnRed());
                        ColorConsole.WriteLine("Channel {0} is {1}".Fmt(channel.UUID, channel.Answered).OnRed());
                    }
                });

            listener.Start();

            Console.WriteLine("Listener started. Press [Enter] to stop");
            Console.ReadLine();

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            listener.Dispose();
        }
    }
}