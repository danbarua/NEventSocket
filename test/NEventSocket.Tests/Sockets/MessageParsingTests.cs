namespace NEventSocket.Tests.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Logging;
    using NEventSocket.Sockets;
    using NEventSocket.Tests.Properties;
    using NEventSocket.Tests.TestSupport;
    using NEventSocket.Util;

    using Xunit;
    using Xunit.Extensions;

    public class MessageParsingTests
    {
        [Theory, PropertyData("ExampleMessages")]
        public void it_should_parse_the_expected_messages_from_a_stream(int expectedMessageCount, string exampleInput)
        {
            int parsedMessageCount = 0;

            exampleInput.ToObservable()
                        .AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                        .Select(parser => parser.ExtractMessage())
                        .Subscribe(_ => parsedMessageCount++);

            Assert.Equal(expectedMessageCount, parsedMessageCount);
        }

        [Theory]
        [InlineData(TestMessages.BackgroundJob)]
        [InlineData(TestMessages.CallState)]
        [InlineData(TestMessages.ConnectEvent)]
        [InlineData(TestMessages.DisconnectEvent)]
        [InlineData(TestMessages.PlaybackComplete)]
        public void can_parse_test_messages(string input)
        {
            var parser = new Parser();
            var rawInput = input.Replace("\r\n", "\n") + "\n\n";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var message = parser.ExtractMessage();
            Assert.NotNull(message);
            Console.WriteLine(message.ToString());
        }

        [Theory]
        [InlineData(TestMessages.BackgroundJob)]
        [InlineData(TestMessages.CallState)]
        public void it_should_extract_the_body_from_a_message(string input)
        {
            var parser = new Parser();
            var rawInput = input.Replace("\r\n", "\n") + "\n\n";
            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            BasicMessage payload = parser.ExtractMessage();
            Assert.Equal(ContentTypes.EventPlain, payload.ContentType);
            Assert.NotNull(payload.BodyText);
            Assert.Equal(payload.ContentLength, payload.BodyText.Length);

            Console.WriteLine(payload.ToString());
        }

        [Theory]
        [InlineData(TestMessages.BackgroundJob, EventName.BackgroundJob)]
        [InlineData(TestMessages.CallState, EventName.ChannelCallstate)]
        public void it_should_parse_event_messages(string input, EventName eventName)
        {
            var parser = new Parser();
            var rawInput = input.Replace("\r\n", "\n") + "\n\n";
            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var eventMessage = new EventMessage(parser.ExtractMessage());
            Assert.NotNull(eventMessage);
            Assert.Equal(eventName, eventMessage.EventName);

            Console.WriteLine(eventMessage.ToString());
        }

        [Fact]
        public void it_should_parse_BackgroundJobResult_OK()
        {
            var input = TestMessages.BackgroundJob;
            var parser = new Parser();
            var rawInput = input.Replace("\r\n", "\n") + "\n\n";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var backroundJobResult = new BackgroundJobResult(new EventMessage(parser.ExtractMessage()));
            Assert.NotNull(backroundJobResult);
            Assert.True(backroundJobResult.Success);

            Console.WriteLine(backroundJobResult.ToString());
        }

        [Fact]
        public void it_should_parse_BackgroundJobResult_ERR()
        {
            var input = TestMessages.BackgroundJobError;
            var parser = new Parser();
            var rawInput = input.Replace("\r\n", "\n") + "\n\n";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var backroundJobResult = new BackgroundJobResult(new EventMessage(parser.ExtractMessage()));
            Assert.NotNull(backroundJobResult);
            Assert.False(backroundJobResult.Success);
            Assert.Equal("Error", backroundJobResult.ErrorMessage);

            Console.WriteLine(backroundJobResult.ToString());
        }

        [Fact]
        public void it_should_parse_Command_Reply_OK()
        {
            var parser = new Parser();
            var rawInput = "Content-Type: command/reply\nReply-Text: +OK\n\n";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }
            
            Assert.True(parser.Completed);

            var reply = new CommandReply(parser.ExtractMessage());
            Assert.NotNull(reply);
            Assert.True(reply.Success);

            Console.WriteLine(reply);
        }

        [Fact]
        public void it_should_parse_Command_Reply_ERR()
        {
            var parser = new Parser();
            var rawInput = "Content-Type: command/reply\nReply-Text: -ERR Error\n\n";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var reply = new CommandReply(parser.ExtractMessage());
            Assert.NotNull(reply);
            Assert.False(reply.Success);
            Assert.Equal("Error", reply.ErrorMessage);

            Console.WriteLine(reply);
        }

        [Fact]
        public void it_should_parse_Api_Response_OK()
        {
            var parser = new Parser();
            var rawInput = "Content-Type: api/response\nContent-Length: 3\n\n+OK";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var response = new ApiResponse(parser.ExtractMessage());
            Assert.NotNull(response);
            Assert.True(response.Success);

            Console.WriteLine(response);
        }

        [Fact]
        public void it_should_parse_Api_Response_ERR()
        {
            var parser = new Parser();
            var rawInput = "Content-Type: api/response\nContent-Length: 10\n\n-ERR Error";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var response = new ApiResponse(parser.ExtractMessage());
            Assert.NotNull(response);
            Assert.False(response.Success);
            Assert.Equal("Error", response.ErrorMessage);

            Console.WriteLine(response);
        }

        [Fact]
        public void it_should_treat_Api_Response_ERR_no_reply_as_Success()
        {
            var parser = new Parser();
            var rawInput = "Content-Type: api/response\nContent-Length: 13\n\n-ERR no reply";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var response = new ApiResponse(parser.ExtractMessage());
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("no reply", response.ErrorMessage);

            Console.WriteLine(response);
        }

        [Fact]
        public void it_should_trim_new_lines_from__the_end_of_ApiResponse_Body_text()
        {
            var parser = new Parser();
            var rawInput = "Content-Type: api/response\nContent-Length: 14\n\n-ERR no reply\n";

            foreach (char c in rawInput)
            {
                parser.Append(c);
            }

            Assert.True(parser.Completed);

            var response = new ApiResponse(parser.ExtractMessage());
            Assert.NotNull(response);
            Assert.True(response.Success);
            Assert.Equal("no reply", response.ErrorMessage);
            Assert.Equal("-ERR no reply", response.BodyText);

            Console.WriteLine(response);
        }

        [Theory]
        [PropertyData("ExampleSessions")]
        public void Can_parse_example_sessions_to_completion(string input)
        {
            if (Environment.GetEnvironmentVariable("APPVEYOR_BUILD_NUMBER") == null)
            {
                LogProvider.SetCurrentLogProvider(new ColouredConsoleLogProvider());
            }

            bool gotDisconnectNotice = false;

            input.ToObservable()
                .AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                .Select(parser => parser.ExtractMessage())
                .Subscribe(
                    m =>
                        {
                            Console.WriteLine(m.ContentType);
                            if (m.ContentType == ContentTypes.DisconnectNotice)
                            {
                                gotDisconnectNotice = true;
                            }
                        });

            Assert.True(gotDisconnectNotice);
        }

        [Fact]
        public void Can_parse_disconnect_notice()
        {
            var msg = @"Content-Type: text/disconnect-notice
Controlled-Session-UUID: 78b86350-4fb8-4d2b-a629-1eeafd7d2f74
Content-Disposition: disconnect
Content-Length: 67

Disconnected, goodbye.
See you at ClueCon! http://www.cluecon.com/
";
            msg.ToObservable()
                .AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                         .Select(parser => parser.ExtractMessage())
                         .Subscribe(
                             Console.WriteLine);
        }

        public static IEnumerable<object[]> ExampleMessages
        {
            get
            {
                yield return new object[] { 0, string.Empty };
                yield return new object[] { 2, "Content-Type: text/event-plain\nEvent-Name: RE_SCHEDULE\nCore-UUID: 6d2375b0-5183-11e1-b24c-f527b57af954\nFreeSWITCH-Hostname: freeswitch.local\nEvent-Date-Local: 2012-02-07 19:36:31\nEvent-Date-GMT: Tue, 07 Feb 2012 18:36:31 GMT\nEvent-Date-Timestamp: 1328639791116026\nEvent-Calling-File: switch_scheduler.c\n\nContent-Type: text/event-plain\nEvent-Name: RE_SCHEDULE\nCore-UUID: 6d2375b0-5183-11e1-b24c-f527b57af954\nFreeSWITCH-Hostname: freeswitch.local\nEvent-Date-Local: 2012-02-07 19:36:31\nEvent-Date-GMT: Tue, 07 Feb 2012 18:36:31 GMT\nEvent-Date-Timestamp: 1328639791116026\nEvent-Calling-File: switch_scheduler.c\n\n" };
                yield return new object[] { 2, "Content-Length: 103\nContent-Type: text/event-plain\n\nEvent-Name: CHANNEL_CALLSTATE\nCore-UUID: f852daae-6da9-4979-8dc8-fa11651a7891\nFreeSWITCH-Hostname: testContent-Length: 29\nContent-Type: text/event-plain\n\nEvent-Name: CHANNEL_CALLSTATE" };
                yield return new object[] { 5, "Content-Type: auth/request\n\nContent-Type: command/reply\nReply-Text: +OK accepted\n\nContent-Type: command/reply\nReply-Text: +OK event listener enabled plain\n\nContent-Type: command/reply\nReply-Text: +OK Job-UUID: 43e14ab9-38b1-4187-9f08-e13f35136bb2\nJob-UUID: 43e14ab9-38b1-4187-9f08-e13f35136bb2\n\nContent-Length: 759\nContent-Type: text/event-plain\n\nEvent-Name: BACKGROUND_JOB\nCore-UUID: ac1ed77b-cf11-4f8a-a36f-3feeb6b53d0e\nFreeSWITCH-Hostname: Dan-MacBook\nFreeSWITCH-Switchname: Dan-MacBook\nFreeSWITCH-IPv4: 192.168.0.6\nFreeSWITCH-IPv6: 2001%3A0%3A5ef5%3A79fd%3A815%3A1a92%3A3f57%3Afff9\nEvent-Date-Local: 2013-06-06%2017%3A24%3A02\nEvent-Date-GMT: Thu,%2006%20Jun%202013%2016%3A24%3A02%20GMT\nEvent-Date-Timestamp: 1370535842909368\nEvent-Calling-File: mod_event_socket.c\nEvent-Calling-Function: api_exec\nEvent-Calling-Line-Number: 1456\nEvent-Sequence: 534\nJob-UUID: 43e14ab9-38b1-4187-9f08-e13f35136bb2\nJob-Command: originate\nJob-Command-Arg: sofia/external/1000%4010.10.10.108%3A5070%20%26playback(C%3A%5C%5Ctemp%5C%5Cmisc-freeswitch_is_state_of_the_art.wav\nContent-Length: 30\n\n-ERR RECOVERY_ON_TIMER_EXPIRE\n"};
            }
        }

        public static IEnumerable<object[]> ExampleSessions
        {
            get
            {
                yield return new object[] { Resources.Example1.Replace("\r\n", "\n") };
                yield return new object[] { Resources.Example2.Replace("\r\n", "\n") };
                yield return new object[] { Resources.Example3.Replace("\r\n", "\n") };
                yield return new object[] { Resources.Example4.Replace("\r\n", "\n") };
                yield return new object[] { Resources.Example5.Replace("\r\n", "\n") };
                yield return new object[] { Resources.Example6.Replace("\r\n", "\n") };
            }
        } 
    }
}