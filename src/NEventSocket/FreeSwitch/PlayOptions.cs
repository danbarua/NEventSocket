// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
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