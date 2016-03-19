// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConferenceEvent.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Defines the ConferenceEvent type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;

    using NEventSocket.Util;

    public class ConferenceEvent : EventMessage
    {
        protected internal ConferenceEvent(EventMessage other) : base(other)
        {
            if (other.EventName != EventName.Custom && other.Headers[HeaderNames.EventSubclass] != CustomEvents.Conference.Maintainence)
            {
                throw new InvalidOperationException(
                    "Expected event of type Custom with SubClass conference::maintainance, got {0} instead".Fmt(other.EventName));
            }
        }

        public ConferenceAction Action
        {
            get
            {
                string headerValue;

                if (Headers.TryGetValue(HeaderNames.Conference.Action, out headerValue))
                {
                    ConferenceAction action;
                    if (Enum.TryParse(headerValue.Replace("-", string.Empty).Replace("_", string.Empty), true, out action))
                    {
                        return action;
                    }
                    throw new NotSupportedException("Unable to parse Conference Action '{0}'.".Fmt(headerValue));
                }

                throw new InvalidOperationException("Event did not contain an Action Header");
            }
        }

        public string Name
        {
            get
            {
                return Headers[HeaderNames.Conference.Name];
            }
        }

        public int Size
        {
            get
            {
                return int.Parse(Headers[HeaderNames.Conference.Size]);
            }
        }

        public string ConferenceUUID
        {
            get
            {
                return Headers[HeaderNames.Conference.ConferenceUniqueId];
            }
        }

        public string MemberId
        {
            get
            {
                return Headers[HeaderNames.Conference.MemberId];
            }
        }

        public string MemberType
        {
            get
            {
                return Headers[HeaderNames.Conference.MemberType];
            }
        }

        public bool Floor
        {
            get
            {
                return bool.Parse(Headers[HeaderNames.Conference.Floor]);
            }
        }

        public bool Video
        {
            get
            {
                return bool.Parse(Headers[HeaderNames.Conference.Video]);
            }
        }

        public bool Hear
        {
            get
            {
                return bool.Parse(Headers[HeaderNames.Conference.Hear]);
            }
        }

        public bool Speak
        {
            get
            {
                return bool.Parse(Headers[HeaderNames.Conference.Speak]);
            }
        }

        public bool Talking
        {
            get
            {
                return bool.Parse(Headers[HeaderNames.Conference.Talking]);
            }
        }

        public bool MuteDetect
        {
            get
            {
                return bool.Parse(Headers[HeaderNames.Conference.MuteDetect]);
            }
        }

        public int EnergyLevel
        {
            get
            {
                return int.Parse(Headers[HeaderNames.Conference.EnergyLevel]);
            }
        }

        public int CurrentEnergy
        {
            get
            {
                return int.Parse(Headers[HeaderNames.Conference.CurrentEnergy]);
            }
        }

        public override string ToString()
        {
            return string.Format("Action: {1}, ConferenceUUID: {2}, CurrentEnergy: {3}, EnergyLevel: {4}, Floor: {5}, Hear: {6}, MemberId: {7}, MemberType: {8}, MuteDetect: {9}, Name: {10}, Size: {11}, Speak: {12}, Talking: {13}, Video: {14}", base.ToString(), Action, ConferenceUUID, CurrentEnergy, EnergyLevel, Floor, Hear, MemberId, MemberType, MuteDetect, Name, Size, Speak, Talking, Video);
        }
    }
}