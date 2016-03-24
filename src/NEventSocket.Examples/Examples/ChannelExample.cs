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
        private OutboundListener listener;

        public Task Run(CancellationToken cancellationToken)
        {
            const string MusicOnHold = "local_stream://moh";

            const string AgentEndPoint = "user/1003";

            const string RingTone = "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1"; // uk ring

            const string DialTone = "tone_stream://%(10000,0,350,440)";

            const string RecordingPath = "/var/tmp/";

            listener = new OutboundListener(8084);

            listener.Channels.Subscribe(
                async channel =>
                {
                    try
                    {
                        channel.BridgedChannels.Subscribe(
                            async bridgedChannel =>
                            {
                                ColorConsole.WriteLine("New Bridged Channel  [{0}]".Fmt(bridgedChannel.UUID).DarkGreen());

                                bridgedChannel.HangupCallBack =
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
                                var recordingPath = RecordingPath + channel.UUID + ".wav";

                                bridgedChannel.FeatureCodes("#").Subscribe(
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
                                                    await channel.Play("ivr/ivr-recording_paused.wav", Leg.BLeg);
                                                    break;
                                                case "#5":
                                                    ColorConsole.WriteLine("Unmask recording".Yellow());
                                                    await channel.UnmaskRecording();
                                                    await channel.Play("ivr/ivr-begin_recording.wav", Leg.BLeg);
                                                    break;
                                                case "#8":
                                                    ColorConsole.WriteLine("Stop recording".Yellow());
                                                    await channel.StopRecording();
                                                    await channel.Play("ivr/ivr-recording_stopped.wav", Leg.Both);
                                                    break;
                                                case "#7":
                                                    ColorConsole.WriteLine("Start recording".Yellow());
                                                    await channel.StartRecording(recordingPath);
                                                    await channel.Play("ivr/ivr-begin_recording.wav", Leg.Both);
                                                    break;
                                                case "#9":
                                                    ColorConsole.WriteLine("Attended x-fer".Yellow());
                                                    await
                                                        Task.WhenAll(
                                                            channel.Play("ivr/ivr-call_being_transferred.wav"),
                                                            bridgedChannel.Play("misc/transfer1.wav"));

                                                    var holdMusic = await channel.PlayUntilCancelled(MusicOnHold);

                                                    var digits =
                                                        await
                                                            bridgedChannel.Read(
                                                                new ReadOptions
                                                                {
                                                                    MinDigits = 3,
                                                                    MaxDigits = 4,
                                                                    Prompt = DialTone,
                                                                    TimeoutMs = 30000,
                                                                    Terminators = "#"
                                                                });

                                                    if (digits.Result != ReadResultStatus.Success || digits.Digits.Length != 4)
                                                    {
                                                        holdMusic.Dispose();
                                                    }
                                                    else
                                                    {
                                                        await bridgedChannel.SetChannelVariable("recording_follow_attxfer", "true");
                                                        await bridgedChannel.SetChannelVariable("origination_cancel_key", "#");
                                                        await bridgedChannel.SetChannelVariable("transfer_ringback", RingTone);

                                                        var xfer = await bridgedChannel.AttendedTransfer("user/{0}".Fmt(digits));
                                                        holdMusic.Dispose();

                                                        ColorConsole.WriteLine(
                                                            "Xfer ".Yellow(),
                                                            xfer.Status.ToString().DarkYellow(),
                                                            " ",
                                                            xfer.HangupCause.GetValueOrDefault().ToString());


                                                        if (xfer.Status != AttendedTransferResultStatus.Failed)
                                                        {
                                                            await channel.Play("misc/transfer2.wav", Leg.Both);
                                                        }
                                                        else
                                                        {
                                                            if (xfer.HangupCause == HangupCause.CallRejected)
                                                            {
                                                                await bridgedChannel.Play("ivr/ivr-call_rejected.wav");
                                                            }
                                                            else if (xfer.HangupCause == HangupCause.NoUserResponse
                                                                     || xfer.HangupCause == HangupCause.NoAnswer)
                                                            {
                                                                await bridgedChannel.Play("ivr/ivr-no_user_response.wav");
                                                            }
                                                            else if (xfer.HangupCause == HangupCause.UserBusy)
                                                            {
                                                                await bridgedChannel.Play("ivr/ivr-user_busy.wav");
                                                            }
                                                            else
                                                            {
                                                                await
                                                                    bridgedChannel.Play(
                                                                        "ivr/ivr-call_cannot_be_completed_as_dialed.wav");
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
                            });

                        channel.HangupCallBack = (e) =>
                        {
                            ColorConsole.WriteLine("Hangup Detected on A-Leg {0} {1}".Fmt(e.Headers[HeaderNames.CallerUniqueId], e.Headers[HeaderNames.HangupCause]).Red());
                            ColorConsole.WriteLine("Aleg bridge {0}".Fmt(channel.Advanced.GetVariable("last_bridge_hangup_cause")).OnRed());
                        };

                        await channel.Answer();

                        var queueHoldMusic = await channel.PlayUntilCancelled(MusicOnHold);

                        await Task.Delay(5000);

                        await channel.Play(new[]
                                           {
                                               "ivr/ivr-you_are_number.wav",
                                               123456.ToFileString(),
                                               "ivr/ivr-in_line.wav"
                                           });

                        await Task.Delay(5000);

                        queueHoldMusic.Dispose();

                        var bridgeOptions = new BridgeOptions()
                        {
                            UUID = Guid.NewGuid().ToString(),
                            IgnoreEarlyMedia = true,
                            RingBack = RingTone,
                            ContinueOnFail = true,
                            HangupAfterBridge = true,
                            TimeoutSeconds = 10,
                            CallerIdName = channel.Advanced.GetVariable("effective_caller_id_name"),
                            CallerIdNumber =
                                                    channel.Advanced.GetVariable("effective_caller_id_number"),
                        };


                        await channel.SetChannelVariable(
                                       "transfer_ringback",
                                       "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1");


                        await channel.BridgeTo(AgentEndPoint, bridgeOptions, (e) => ColorConsole.WriteLine("Bridge Progress Ringing...".DarkGreen()));

                        if (!channel.IsBridged)
                        {
                            ColorConsole.WriteLine("Bridge Failed - {0}".Fmt(channel.Advanced.GetVariable("last_bridge_hangup_cause")).Red());
                            await channel.Play("ivr/ivr-call_rejected.wav");
                            await channel.Hangup(HangupCause.NormalTemporaryFailure);
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