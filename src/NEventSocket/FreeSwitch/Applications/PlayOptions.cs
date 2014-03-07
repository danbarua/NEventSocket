namespace NEventSocket.FreeSwitch.Applications
{
    public class PlayOptions
    {
        private int loops = 1;

        public int Loops
        {
            get
            {
                return this.loops;
            }
            set
            {
                this.loops = value;
            }
        }

        public Leg Leg { get; set; }
    }
}