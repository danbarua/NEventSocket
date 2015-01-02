namespace Channels
{
    using System;
    using System.Threading.Tasks;

    using ColoredConsole;

    using NEventSocket;
    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    internal class ChannelExample
    {
        public void Run()
        {
            var listener = new OutboundListener(8084);

            listener.Channels.Subscribe(
                async channel =>
                    {
                        try
                        {
                            channel.HangupCallBack = (e) =>
                                {
                                    ColorConsole.WriteLine("Hangup Detected on A-Leg {0} {1}".Fmt(e.Headers[HeaderNames.CallerUniqueId], e.Headers[HeaderNames.HangupCause]).Red());
                                    ColorConsole.WriteLine("Aleg bridge {0}".Fmt(channel.Bridge.HangupCause).OnRed());
                                };

                            await channel.Answer();
                            await channel.PlayFile("ivr/8000/ivr-call_being_transferred.wav");
                            await channel.StartDetectingInbandDtmf();

                            var bridgeOptions = new BridgeOptions()
                                                    {
                                                        UUID = Guid.NewGuid().ToString(),
                                                        IgnoreEarlyMedia = true,
                                                        RingBack =
                                                            "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1",
                                                        ContinueOnFail = true,
                                                        HangupAfterBridge = true,
                                                        TimeoutSeconds = 60,
                                                        CallerIdName = channel["effective_caller_id_name"],
                                                        CallerIdNumber = channel["effective_caller_id_number"],
                                                    };

                            bridgeOptions.ChannelVariables.Add("bridge_filter_dtmf", "true");

                            await channel.BridgeTo("user/1003", bridgeOptions, (e) => ColorConsole.WriteLine("Bridge Progress Ringing...".DarkGreen()));

                            if (!channel.Bridge.IsBridged)
                            {
                                ColorConsole.WriteLine("Bridge Failed - {0}".Fmt(channel.Bridge.HangupCause).Red());
                                await channel.PlayFile("ivr/8000/ivr-call_rejected.wav");
                                await channel.Hangup(HangupCause.NormalTemporaryFailure);
                            }
                            else
                            {
                                ColorConsole.WriteLine("Bridge success - {0}".Fmt(channel.Bridge.ResponseText).DarkGreen());

                                channel.Bridge.Channel.HangupCallBack = (e) => ColorConsole.WriteLine("Hangup Detected on B-Leg {0} {1}".Fmt(e.Headers[HeaderNames.CallerUniqueId], e.Headers[HeaderNames.HangupCause]).Red());

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
                                                    await channel.PlayFile(
                                                        "ivr/8000/ivr-recording_stopped.wav", Leg.Both);
                                                    break;
                                                case "#7":
                                                    ColorConsole.WriteLine("Start recording".Yellow());
                                                    await channel.StartRecording(recordingPath);
                                                    await channel.PlayFile("ivr/8000/ivr-begin_recording.wav", Leg.Both);
                                                    break;
                                                case "#9":
                                                    ColorConsole.WriteLine("Attended x-fer".Yellow());
                                                    var digits = await channel.Bridge.Channel.Read(new ReadOptions { MinDigits = 3, MaxDigits = 4, Prompt = "tone_stream://%(10000,0,350,440)", TimeoutMs = 30000, Terminators = "#" });
                                                    if (digits.Result == ReadResultStatus.Success && digits.Digits.Length == 4)
                                                    {
                                                        //todo: set channel variable "recording_follow_attxfer" <-- test it out!
                                                        await channel.Bridge.Channel.SetChannelVariable("recording_follow_attxfer", "true");
                                                        await channel.Bridge.Channel.SetChannelVariable("origination_cancel_key", "#");
                                                        await channel.Bridge.Channel.SetChannelVariable("transfer_ringback", "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1");
                                                        await channel.Bridge.Channel.AttendedTransfer("user/{0}".Fmt(digits));

                                                        ColorConsole.WriteLine("Transfer completed att_xfer_result: ".OnDarkBlue(),channel.Bridge.Channel["att_xfer_result"].Blue());
                                                        ColorConsole.WriteLine("originate_disposition: ".OnDarkBlue(), channel.Bridge.Channel["originate_disposition"].Blue());
                                                        ColorConsole.WriteLine("Xfer uuids".OnDarkBlue(), channel.Bridge.Channel["xfer_uuids"].Blue());

                                                        ColorConsole.WriteLine("att_xfer_result: ".OnDarkGreen(), channel["att_xfer_result"].DarkGreen());
                                                        ColorConsole.WriteLine("originate_disposition: ".OnDarkGreen(), channel["originate_disposition"].DarkGreen());
                                                        ColorConsole.WriteLine("Xfer uuids".OnDarkGreen(), channel.Bridge.Channel["xfer_uuids"].DarkGreen());

                                                        //todo: see variable "att_xfer_result" == "success" or "failure"
                                                        //todo: see variable "xfer_uuids"
                                                        //todo: inspect hangup state of the b-leg CHANNEL_EXECUTE_COMPLETE event
                                                        //and determine if the b-leg hung up or is still connected
                                                        //if b-leg hung up, is there any way to get a reference to the c-leg?
                                                        //if so, we need to replace the bridgedUUID with the c-leg
                                                        //we currently get a UNBRIDGE event from the b-leg but not
                                                        //a  bridge event for the c-leg
                                                        //also: what happens when we transfer to a 3-way? the 'bridge' completes
                                                        //but A, B and C are bridged together somehow.
                                                    }

                                                    break;
                                            }
                                        });
                            }
                        }
                        catch (TaskCanceledException ex)
                        {
                            ColorConsole.WriteLine("TaskCancelled - shutting down\r\n{0}".Fmt(ex.ToString()).OnRed());
                            ColorConsole.WriteLine("Channel {0} is {1}".Fmt(channel.UUID, channel.Answered).OnRed());
                            //aLeg.Dispose();
                        }
                    });

            listener.Start();
        }
    }
}