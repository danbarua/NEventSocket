namespace NEventSocket.Tests.Applications
{
    using NEventSocket.FreeSwitch;

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

             options.EnterpriseChannelVariables.Add("fooE", "barE");
             options.EnterpriseChannelVariables.Add("bazE", "widgetsE");
             options.ChannelVariables.Add("foo", "bar");
             options.ChannelVariables.Add("baz", "widgets");

             var toString = options.ToString();

             const string Expected =
                 "<fooE='barE',bazE='widgetsE'>{origination_uuid='985cea12-4e70-4c03-8a2c-2c4b4502bbbb',bypass_media='true',origination_caller_id_name='Test',origination_caller_id_number='12341234',execute_on_originate='start_dtmf',ignore_early_media='true',originate_retries='3',originate_retry_sleep_ms='4000',return_ring_ready='true',originate_timeout='20',hangup_after_bridge='false',foo='bar',baz='widgets'}";
             Assert.Equal(Expected, toString);
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