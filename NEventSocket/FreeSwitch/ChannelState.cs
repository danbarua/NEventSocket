// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelState.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Represents the state of a Channel
    /// </summary>
    public enum ChannelState
    {
#pragma warning disable 1591
        New, 

        Init, 

        Routing, 

        SoftExecute, 

        Execute, 

        ExchangeMedia, 

        Park, 

        ConsumeMedia, 

        Hibernate, 

        Reset, 

        Hangup, 

        Done,

        Destroy,

        Reporting,

        None
#pragma warning restore 1591
    }
}