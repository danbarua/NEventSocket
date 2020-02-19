// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Options for customizing the behaviour of the Play dialplan application
    /// </summary>
    public class PlayOptions
    {
        private int loops = 1;

        /// <summary>
        /// Gets or sets the number of repetitions to play (default 1).
        /// </summary>
        public int Loops
        {
            get
            {
                return loops;
            }

            set
            {
                loops = value;
            }
        }
    }
}