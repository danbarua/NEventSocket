// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILogProvider.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   The LogProvider interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Logging
{
    /// <summary>The LogProvider interface.</summary>
    public interface ILogProvider
    {
        /// <summary>The get logger.</summary>
        /// <param name="name">The name.</param>
        /// <returns>The <see cref="ILog"/>.</returns>
        ILog GetLogger(string name);
    }
}