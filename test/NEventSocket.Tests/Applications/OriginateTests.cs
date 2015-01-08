namespace NEventSocket.Tests
{
    using System;

    using NEventSocket.FreeSwitch;

    using Xunit;

    public class OriginateTests
    {
        [Fact]
        public void Can_format_originate_options()
        {
            var options = new OriginateOptions()
                              {
                                  CallerIdName = "Dan",
                                  CallerIdNumber = "0123457890",
                                  ExecuteOnOriginate = "my_app::my_arg",
                                  Retries = 5,
                                  RetrySleepMs = 200,
                                  ReturnRingReady = true,
                                  TimeoutSeconds = 60,
                                  UUID = "83fe4f3d-b957-4b26-b6bf-3879d7e21972",
                                  IgnoreEarlyMedia = true,
                              };

            Assert.Equal(
                "{origination_caller_id_name='Dan',origination_caller_id_number='0123457890',execute_on_originate='my_app::my_arg',originate_retries='5',originate_retry_sleep_ms='200',return_ring_ready='true',originate_timeout='60',origination_uuid='83fe4f3d-b957-4b26-b6bf-3879d7e21972',ignore_early_media='true'}",
                options.ToString());
        }

        [Fact]
        public void can_set_caller_id_type()
        {
            var options = new OriginateOptions() { SipCallerIdType = SipCallerIdType.RPid }.ToString();
            Assert.Contains("sip_cid_type='rpid'", options);
        }

        [Fact]
        public void can_set_privacy()
        {
            var options = new OriginateOptions() { OriginationPrivacy = OriginationPrivacy.HideName | OriginationPrivacy.HideNumber | OriginationPrivacy.Screen}.ToString();
            Assert.Contains("origination_privacy='hide_name:hide_number:screen'", options);
        }
    }
}