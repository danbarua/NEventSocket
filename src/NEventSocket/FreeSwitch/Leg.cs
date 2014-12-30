// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Leg.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Describes a leg of a call consisting of two channels bridged together.
    /// Used when playing audio into a bridged call.
    /// </summary>
    public enum Leg
    {
        /// <summary>
        /// Play to both legs of the call
        /// </summary>
        Both, 

        /// <summary>
        /// Play to the A-Leg of the call
        /// </summary>
        ALeg, 

        /// <summary>
        /// Play to the B-Leg of the call
        /// </summary>
        BLeg, 
    }
}