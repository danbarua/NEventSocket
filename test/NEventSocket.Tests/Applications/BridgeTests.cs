namespace NEventSocket.Tests
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    using NEventSocket.FreeSwitch;

    using Xunit;

    public class BridgeTests
    {
        [Fact]
        public void can_format_BridgeOptions()
        {
            var options = new BridgeOptions()
            {
                UUID = "985cea12-4e70-4c03-8a2c-2c4b4502bbbb",
                TimeoutSeconds = 20,
                CallerIdName = "Dan B Leg",
                CallerIdNumber = "987654321",
                HangupAfterBridge = false,
                IgnoreEarlyMedia = true,
                ContinueOnFail = true,
                RingBack = "${uk-ring}"
            };

            // channel variables have no effect on ToString(), they're set on the a-leg of the call before initiating the bridge.
            // todo: allow exporting variables?
            options.ChannelVariables.Add("foo", "bar");
            options.ChannelVariables.Add("baz", "widgets");

            var toString = options.ToString();
            const string Expected = "{origination_uuid='985cea12-4e70-4c03-8a2c-2c4b4502bbbb',leg_timeout='20',origination_caller_id_name='Dan B Leg',origination_caller_id_number='987654321',ignore_early_media='true'}";
            Assert.Equal(Expected, toString);
        }

        [Fact]
        public void can_serialize_and_deserialize_BridgeOptions()
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();

                var options = new BridgeOptions()
                {
                    UUID = "985cea12-4e70-4c03-8a2c-2c4b4502bbbb",
                    TimeoutSeconds = 20,
                    CallerIdName = "Dan B Leg",
                    CallerIdNumber = "987654321",
                    HangupAfterBridge = false,
                    IgnoreEarlyMedia = true,
                    ContinueOnFail = true,
                    RingBack = "${uk-ring}"
                };

                options.ChannelVariables.Add("foo", "bar");
                options.ChannelVariables.Add("baz", "widgets");

                formatter.Serialize(ms, options);

                ms.Seek(0, SeekOrigin.Begin);

                var fromStream = formatter.Deserialize(ms) as BridgeOptions;
                Assert.Equal(options, fromStream);
            }
        }
    }
}