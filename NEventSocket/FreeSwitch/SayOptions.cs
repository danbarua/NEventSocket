// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SayOptions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.FreeSwitch
{
    using NEventSocket.Util;

    /// <summary>
    /// Represents options to pass to the Say dialplan application.
    /// </summary>
    public class SayOptions
    {
        private string moduleName = "en";

        private SayGender gender = SayGender.Neuter;

        /// <summary>
        /// Module name is usually the channel language, e.g. "en" or "es" 
        /// </summary>
        public string ModuleName
        {
            get
            {
                return moduleName;
            }

            set
            {
                moduleName = value;
            }
        }

        /// <summary>
        /// The Say Module to use
        /// </summary>
        public SayType Type { get; set; }

        /// <summary>
        /// The Say Method
        /// </summary>
        /// <remarks>
        /// Say method is one of the following (for example, passing a value of "42"):
        /// pronounced - e.g. "forty two"
        /// iterated - e.g. "four two"
        /// counted - e.g. "forty second"
        /// </remarks>
        public SayMethod Method { get; set; }

        /// <summary>
        /// For languages with gender-specific grammar like French and German
        /// </summary>
        public SayGender Gender
        {
            get
            {
                return gender;
            }

            set
            {
                gender = value;
            }
        }

        /// <summary>
        /// Gets or sets the text to say
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Converts the <seealso cref="SayOptions"/> instance into an application argument string.
        /// </summary>
        /// <returns>An application argument string.</returns>
        public override string ToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4}",
                ModuleName,
                Type.ToString().ToUpperWithUnderscores().ToUpperInvariant(),
                Method.ToString().ToLowerInvariant(),
                Gender.ToString().ToUpperInvariant(),
                Text);
        }
    }
}