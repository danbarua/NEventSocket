// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Leg.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Refers to a leg of a call consisting of one or more channels
    /// </summary>
    public enum Leg
    {
        /// <summary>
        /// Both Legs of the Call
        /// </summary>
        Both, 

        /// <summary>
        /// The A-Leg of the call
        /// </summary>
        ALeg, 

        /// <summary>
        /// The B-Leg of the call
        /// </summary>
        BLeg, 
    }
}