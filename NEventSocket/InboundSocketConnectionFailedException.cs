
namespace NEventSocket
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Wraps errors caught when attempting to create an <seealso cref="InboundSocket"/> connection.
    /// </summary>
    [Serializable]
    public class InboundSocketConnectionFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InboundSocketConnectionFailedException"/> class.
        /// </summary>
        public InboundSocketConnectionFailedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InboundSocketConnectionFailedException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public InboundSocketConnectionFailedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InboundSocketConnectionFailedException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public InboundSocketConnectionFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InboundSocketConnectionFailedException"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="context">The context.</param>
        protected InboundSocketConnectionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
