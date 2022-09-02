// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReadResultStatus.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Represents the Status of a Read Result
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Represents the Status of a Read Result
    /// </summary>
    public enum ReadResultStatus
    {
#pragma warning disable 1591
        Success,

        Timeout,

        Failure
#pragma warning restore 1591
    }
}