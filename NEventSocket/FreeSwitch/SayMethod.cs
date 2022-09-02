// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SayMethod.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// The Method to use with the Say dialplan application
    /// </summary>
    public enum SayMethod
    {
#pragma warning disable 1591
        Pronounced, 

        Iterated,

        Counted
#pragma warning restore 1591
    }
}