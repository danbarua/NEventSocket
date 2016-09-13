// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayResult.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System.IO;

    /// <summary>
    /// Represents the result of the Play dialplan application
    /// </summary>
    public class PlayResult : ApplicationResult
    {
        internal PlayResult(ChannelEvent eventMessage) : base(eventMessage)
        {
            if (eventMessage != null)
            {
                Success = ResponseText == "FILE PLAYED";  //eventMessage.Headers[HeaderNames.ApplicationResponse] == "FILE PLAYED";
            }
            else
            {
                Success = false;
            }
        }
    }
}