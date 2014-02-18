namespace NEventSocket.Specs.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

    using FluentAssertions;

    using NEventSocket.Messages;
    using NEventSocket.Sockets.Protocol;

    using Xbehave;

    public class ParserScenarios
    {
        private readonly List<BasicMessage> messages  = new List<BasicMessage>();

        [Scenario]
        public void messages_with_headers_only(string stream)
        {
            "Given a stream"._(() => stream = "Content-Type: text/event-plain\nEvent-Name: RE_SCHEDULE\nCore-UUID: 6d2375b0-5183-11e1-b24c-f527b57af954\nFreeSWITCH-Hostname: freeswitch.local\nEvent-Date-Local: 2012-02-07 19:36:31\nEvent-Date-GMT: Tue, 07 Feb 2012 18:36:31 GMT\nEvent-Date-Timestamp: 1328639791116026\nEvent-Calling-File: switch_scheduler.c\n\nContent-Type: text/event-plain\nEvent-Name: RE_SCHEDULE\nCore-UUID: 6d2375b0-5183-11e1-b24c-f527b57af954\nFreeSWITCH-Hostname: freeswitch.local\nEvent-Date-Local: 2012-02-07 19:36:31\nEvent-Date-GMT: Tue, 07 Feb 2012 18:36:31 GMT\nEvent-Date-Timestamp: 1328639791116026\nEvent-Calling-File: switch_scheduler.c\n\n");

            "When the stream is parsed"._(
                () =>
                stream.ToObservable()
                      .ExtractBasicMessages()
                      .Subscribe(m => messages.Add(m)));

            "Then it should have parsed 2 messages"._(() => messages.Count.ShouldBeEquivalentTo(2));

            "And the message BodyText should be null"._(() => messages.ForEach((m) => m.BodyText.Should().BeNull()));
        }

        [Scenario]
        public void messages_with_bodies(string stream)
        {
            "Given a stream"._(() => stream = "Content-Length: 103\nContent-Type: text/event-plain\n\nEvent-Name: CHANNEL_CALLSTATE\nCore-UUID: f852daae-6da9-4979-8dc8-fa11651a7891\nFreeSWITCH-Hostname: testContent-Length: 29\nContent-Type: text/event-plain\n\nEvent-Name: CHANNEL_CALLSTATE");

            "When the stream is parsed"._(
                () =>
                stream.ToObservable()
                      .ExtractBasicMessages()
                      .Subscribe(m => messages.Add(m)));

            "Then it should have parsed 2 messages"._(() => messages.Count.ShouldBeEquivalentTo(2));

            "And the message BodyText should not be null or empty"._(() => messages.ForEach((m) => m.BodyText.Should().NotBeEmpty()));
        }

        [Scenario]
        public void mixed_messages(string stream)
        {
            "Given a stream"._(() => stream = "Content-Type: auth/request\n\nContent-Type: command/reply\nReply-Text: +OK accepted\n\nContent-Type: command/reply\nReply-Text: +OK event listener enabled plain\n\nContent-Type: command/reply\nReply-Text: +OK Job-UUID: 43e14ab9-38b1-4187-9f08-e13f35136bb2\nJob-UUID: 43e14ab9-38b1-4187-9f08-e13f35136bb2\n\nContent-Length: 759\nContent-Type: text/event-plain\n\nEvent-Name: BACKGROUND_JOB\nCore-UUID: ac1ed77b-cf11-4f8a-a36f-3feeb6b53d0e\nFreeSWITCH-Hostname: Dan-MacBook\nFreeSWITCH-Switchname: Dan-MacBook\nFreeSWITCH-IPv4: 192.168.0.6\nFreeSWITCH-IPv6: 2001%3A0%3A5ef5%3A79fd%3A815%3A1a92%3A3f57%3Afff9\nEvent-Date-Local: 2013-06-06%2017%3A24%3A02\nEvent-Date-GMT: Thu,%2006%20Jun%202013%2016%3A24%3A02%20GMT\nEvent-Date-Timestamp: 1370535842909368\nEvent-Calling-File: mod_event_socket.c\nEvent-Calling-Function: api_exec\nEvent-Calling-Line-Number: 1456\nEvent-Sequence: 534\nJob-UUID: 43e14ab9-38b1-4187-9f08-e13f35136bb2\nJob-Command: originate\nJob-Command-Arg: sofia/external/1000%4010.10.10.108%3A5070%20%26playback(C%3A%5C%5Ctemp%5C%5Cmisc-freeswitch_is_state_of_the_art.wav\nContent-Length: 30\n\n-ERR RECOVERY_ON_TIMER_EXPIRE\n");

            "When the stream is parsed"._(
                () =>
                stream.ToObservable()
                      .ExtractBasicMessages()
                      .Subscribe(m => messages.Add(m)));

            "Then it should have parsed 5 messages"._(() => messages.Count.ShouldBeEquivalentTo(5));
        }
    }
}