// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContentTypes.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Contains well-known Content Types for a <seealso cref="BasicMessage"/>.
    /// </summary>
    public static class ContentTypes
    {
#pragma warning disable 1591
        public const string EventPlain = "text/event-plain";

        public const string EventJson = "text/event-json";

        public const string EventXml = "text/event-xml";

        public const string AuthRequest = "auth/request";

        public const string CommandReply = "command/reply";

        public const string ApiResponse = "api/response";

        public const string DisconnectNotice = "text/disconnect-notice";

        public const string RudeRejection = "text/rude-rejection";
#pragma warning restore 1591
    }
}