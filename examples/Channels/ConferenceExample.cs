namespace Channels
{
    using System;
    using System.Reactive.Linq;

    using ColoredConsole;

    using NEventSocket;
    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    internal class ConferenceExample
    {
        public void Run()
        {
            const string TempFolder = "C:/temp/";
            const string ConferenceId = "my-test-conference";
            const string ConferencePin = "1234";
            const string ConferenceArgs = ConferenceId;// +"+" + ConferencePin;

            var listener = new OutboundListener(8084);
            string conferenceServerIp = null;
            bool conferenceIsStarted = false;

            listener.Channels.Subscribe(
                async channel =>
                {
                    try
                    {
                        var serverIpAddress = channel.Advanced.GetHeader("FreeSWITCH-IPv4");
                        var destinationNumber = channel.Advanced.GetHeader("Channel-Destination-Number");

                        ColorConsole.WriteLine("Connection from server ", serverIpAddress.Blue(), " for number", destinationNumber.Blue());

                        if (conferenceServerIp != null && conferenceServerIp != serverIpAddress)
                        {
                            //the conference has started on a different server, redirect to that server
                            await channel.Execute("redirect", "sip:" + destinationNumber + "@" + conferenceServerIp);
                        }
                        else
                        {
                            //either conference has not started yet or it has started on this server
                            await channel.Answer();
                            await channel.Sleep(400);
                            await channel.PlayFile("ivr/ivr-welcome_to_freeswitch.wav");

                            if (conferenceIsStarted)
                            {
                                //prompt user for their name

                                var nameFile = string.Concat(TempFolder, channel.UUID, ".wav");
                                ColorConsole.WriteLine("Recording name file to ", nameFile.Blue());


                                await channel.PlayFile("ivr/ivr-say_name.wav");
                                await channel.PlayFile("tone_stream://%(500,0,500)");
                                await channel.Advanced.Socket.ExecuteApplication(channel.UUID, "record", nameFile + " 10 200 1");
                                await
                                    channel.Advanced.Socket.Api(
                                        "sched_api +1 none conference {0} play file_string://{1}!conference/conf-has_joined.wav".Fmt(ConferenceId, nameFile));
                            }
                            else
                            {
                                //first person in the conference, no need to record their name
                                conferenceIsStarted = true;
                                conferenceServerIp = serverIpAddress;
                            }

                            channel.Advanced.Socket.ConferenceEvents
                                .Subscribe(x =>
                                    {
                                        ColorConsole.WriteLine("Got conf event ".DarkYellow(), x.Action.ToString().Yellow());
                                        if (x.Action == ConferenceAction.StartTalking)
                                        {
                                            ColorConsole.WriteLine("Channel ".DarkGreen(), x.UUID.Green(), " started talking".DarkGreen());
                                        }

                                        if (x.Action == ConferenceAction.StopTalking)
                                        {
                                            ColorConsole.WriteLine("Channel ".DarkRed(), x.UUID.Red(), " stopped talking".DarkRed());
                                        }
                                    });

                            //if we await the result of this, we'll get OperationCanceledException on hangup
                            await channel.Advanced.Socket.ExecuteApplication(channel.UUID, "conference", ConferenceArgs);
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        ColorConsole.WriteLine("TaskCancelled - shutting down\r\n{0}".Fmt(ex.ToString()).OnRed());
                        ColorConsole.WriteLine("Channel {0} is {1}".Fmt(channel.UUID, channel.Answered).OnRed());
                    }
                });

            listener.Start();
        }
    }
}