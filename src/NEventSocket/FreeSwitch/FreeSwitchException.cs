// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FreeSwitchException.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class FreeSwitchException : Exception
    {
        public FreeSwitchException()
        {
        }

        public FreeSwitchException(string message) : base(message)
        {
        }

        public FreeSwitchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected FreeSwitchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}