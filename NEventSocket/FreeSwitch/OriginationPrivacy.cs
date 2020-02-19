// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OriginationPrivacy.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the Origination Privacy
//   See https://wiki.freeswitch.org/wiki/Variable_origination_privacy
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

#pragma warning disable 1591
    /// <summary>
    /// Defines the Origination Privacy
    /// See https://wiki.freeswitch.org/wiki/Variable_origination_privacy
    /// </summary>
    [Flags]
    public enum OriginationPrivacy
    {
        HideName,
        HideNumber,
        Screen
    }
#pragma warning restore 1591
}