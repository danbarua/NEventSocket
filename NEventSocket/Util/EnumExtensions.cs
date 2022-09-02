// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// <summary>
//   Extension methods for Enums
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods for Enums
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the unique flags of a flag Enum
        /// </summary>
        public static IEnumerable<Enum> GetUniqueFlags(this Enum flags)
        {
            return Enum.GetValues(flags.GetType()).Cast<Enum>().Where(flags.HasFlag);
        }
    }
}