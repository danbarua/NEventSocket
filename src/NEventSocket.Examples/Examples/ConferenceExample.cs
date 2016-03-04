namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    public class ConferenceExample : ICommandLineTask, IDisposable

    {
        private OutboundListener listener;

        public Task Run(CancellationToken cancellationToken)
        {
            const string TempFolder = "C:/temp/";
            const string ConferenceId = "my-test-conference";
            const string ConferencePin = "1234";
            const string ConferenceArgs = ConferenceId;// +"+" + ConferencePin;

            this.listener = new OutboundListener(8084);
            string conferenceServerIp = null;
            bool conferenceIsStarted = false;

            this.listener.Channels.Subscribe(
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

                            await channel.Advanced.Socket.SubscribeCustomEvents(CustomEvents.Conference.Maintainence);

                            if (conferenceIsStarted)
                            {
                                //prompt user for their name

                                var nameFile = string.Concat(TempFolder, channel.UUID, ".wav");
                                ColorConsole.WriteLine("Recording name file to ", nameFile.Blue());


                                await channel.PlayFile("ivr/ivr-say_name.wav");
                                await channel.PlayFile("tone_stream://%(500,0,500)");
                                await channel.Advanced.Socket.ExecuteApplication(channel.UUID, "record", nameFile + " 10 200 1");

                                //when this member enters the conference, play the announcement
                                channel.Advanced.Socket.ConferenceEvents.FirstAsync(x => x.Action == ConferenceAction.AddMember)
                                    .Subscribe(
                                        _ => channel.Advanced.Socket.Api("conference {0} play file_string://{1}!conference/conf-has_joined.wav"
                                            .Fmt(ConferenceId, nameFile)));
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
                                    switch (x.Action)
                                    {
                                        case ConferenceAction.StartTalking:
                                            ColorConsole.WriteLine(
                                                "Channel ".DarkGreen(), x.UUID.Green(), " started talking".DarkGreen());
                                            break;
                                        case ConferenceAction.StopTalking:
                                            ColorConsole.WriteLine("Channel ".DarkRed(), x.UUID.Red(), " stopped talking".DarkRed());
                                            break;
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

            this.listener.Start();
                
            Console.WriteLine("Listener started. Press [Enter] to stop");
            Console.ReadLine();

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            this.listener.Dispose();
        }
    }
}