// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChannelState.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    public enum ChannelState
    {
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
    }
}