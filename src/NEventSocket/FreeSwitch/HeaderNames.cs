// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeaderNames.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// Provides well-known message header names
    /// </summary>
    public static class HeaderNames
    {
#pragma warning disable 1591
        public const string ContentLength = "Content-Length";

        public const string ContentType = "Content-Type";

        public const string CallerUniqueId = "Caller-Unique-ID";

        /// <summary>
        /// In a CHANNEL_EXECUTE_COMPLETE, contains the application that was executed
        /// </summary>
        public const string Application = "Application";

        /// <summary>
        /// In a CHANNEL_EXECUTE_COMPLETE event, contains the args passed to the application
        /// </summary>
        public const string ApplicationData = "Application-Data";

        /// <summary>
        /// In a CHANNEL_EXECUTE_COMPLETE event, contains the response from the application
        /// </summary>
        public const string ApplicationResponse = "Application-Response";

        public const string EventName = "Event-Name";

        public const string ChannelState = "Channel-State";

        public const string AnswerState = "Answer-State";

        public const string HangupCause = "Hangup-Cause";

        public const string EventSubclass = "Event-Subclass";

        public const string UniqueId = "Unique-ID";

        public const string OtherLegUniqueId = "Other-Leg-Unique-ID";

        public const string ChannelCallUniqueId = "Channel-Call-UUID";

        public const string JobUUID = "Job-UUID";

        public const string ReplyText = "Reply-Text";

        public const string DtmfDigit = "DTMF-Digit";

        public static class Conference
        {
            public const string Name = "Conference-Name";

            public const string Size = "Conference-Size";

            public const string ProfileName = "Conference-Profile-Name";

            public const string ConferenceUniqueId = "Conference-Unique-ID";

            public const string Floor = "Floor";

            public const string Video = "Video";

            public const string Hear = "Hear";

            public const string Speak = "Speak";

            public const string Talking = "Talking";

            public const string MuteDetect = "Mute-Detect";

            public const string MemberId = "Member-ID";

            public const string MemberType = "Member-Type";

            public const string EnergyLevel = "Energy-Level";

            public const string CurrentEnergy = "Current-Energy";

            public const string Action = "Action";
        }
#pragma warning restore 1591
    }
}