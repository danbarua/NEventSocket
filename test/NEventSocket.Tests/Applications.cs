namespace NEventSocket.Tests
{
    using NEventSocket.FreeSwitch.Api;
    using NEventSocket.FreeSwitch.Applications;

    using Xunit;

    public class Applications
    {
         [Fact]
         public void can_build_say_string()
         {
             var options = new SayOptions
                               {
                                   ModuleName = "en",
                                   Gender = SayGender.Feminine,
                                   Method = SayMethod.Iterated,
                                   Type = SayType.Number,
                                   Text = "1234"
                               };

             var toString = options.ToString();

             Assert.Equal("en NUMBER iterated FEMININE 1234", toString);
         }

         [Fact]
         public void can_build_originate_string()
         {
             var options = new OriginateOptions()
                               {
                                   UUID = "985cea12-4e70-4c03-8a2c-2c4b4502bbbb",
                                   BypassMedia = true,
                                   CallerIdName = "Test",
                                   CallerIdNumber = "12341234",
                                   ExecuteOnOriginate = "start_dtmf",
                                   HangupAfterBridge = false,
                                   IgnoreEarlyMedia = true,
                                   Retries = 3,
                                   RetrySleepMs = 4000,
                                   ReturnRingReady = true,
                                   TimeoutSeconds = 20
                               };

             var toString = options.ToString();

             Assert.Equal("{origination_uuid='985cea12-4e70-4c03-8a2c-2c4b4502bbbb',origination_caller_id_name='Test',origination_caller_id_number=12341234,execute_on_originate='start_dtmf',originate_retries=3,originate_retry_sleep_ms=4000,originate_timeout=20,return_ring_ready=true,ignore_early_media=true,bypass_media=true,hangup_after_bridge=false}", toString);
         }

        [Fact]
        public void can_build_bridge_string()
        {
            var options = new BridgeOptions()
                              {
                                  UUID = "985cea12-4e70-4c03-8a2c-2c4b4502bbbb",
                                  Timeout = 20,
                                  CallerIdName = "Dan B Leg",
                                  CallerIdNumber = "987654321",
                                  HangupAfterBridge = false,
                                  IgnoreEarlyMedia = true,
                                  ContinueOnFail = true,
                                  RingBack = "${uk-ring}"
                              };

            var toString = options.ToString();

            Assert.Equal("{call_timeout=20,origination_uuid='985cea12-4e70-4c03-8a2c-2c4b4502bbbb',origination_caller_id_name='Dan B Leg',origination_caller_id_number=987654321,originate_timeout=20,ignore_early_media=true}", toString);
        }

        [Fact]
        public void can_build_play_get_digits_string()
        {
            var options = new PlayGetDigitsOptions()
                              {
                                  MinDigits = 4,
                                  MaxDigits = 8,
                                  MaxTries = 3,
                                  TimeoutMs = 4000,
                                  TerminatorDigits = "#",
                                  PromptAudioFile =
                                      "ivr/8000/ivr-please_enter_pin_followed_by_pound.wav",
                                  BadInputAudioFile = "ivr/8000/ivr-that_was_an_invalid_entry.wav",
                                  DigitTimeoutMs = 2000,
                                  ValidDigits = "1234567890" //note that in the command string this gets transformed into the regex ^(1|2|3|4|5|6|7|8|9|0)+
                              };

            var toString = options.ToString();

            Assert.Equal(@"4 8 3 4000 # ivr/8000/ivr-please_enter_pin_followed_by_pound.wav ivr/8000/ivr-that_was_an_invalid_entry.wav play_get_digits_result ^(1|2|3|4|5|6|7|8|9|0)+ 2000", toString);
          }
    }
}