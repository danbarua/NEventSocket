namespace NEventSocket.FreeSwitch.Applications
{
    using System.IO;

    public enum Leg
    {
        Both,
        ALeg,
        BLeg,
    }

    public class PlayOptions
    {
        public Leg Leg { get; set; }
    }

    public class PlayResult : ApplicationResult
    {
        public PlayResult(EventMessage eventMessage)
            : base(eventMessage)
        {
            if (eventMessage.Headers[HeaderNames.ApplicationResponse] == "FILE NOT FOUND")
                throw new FileNotFoundException("FreeSwitch was unable to play the file.", eventMessage.Headers[HeaderNames.ApplicationData]);

            this.Success = eventMessage.Headers[HeaderNames.ApplicationResponse] == "FILE PLAYED";
        }
    }
}