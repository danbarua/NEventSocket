// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SayOptions.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch.Applications
{
    using NEventSocket.Util;

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
                return this.moduleName;
            }

            set
            {
                this.moduleName = value;
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
                return this.gender;
            }

            set
            {
                this.gender = value;
            }
        }

        public string Text { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} {4}", ModuleName, Type.ToString().ToUpperWithUnderscores().ToUpperInvariant(), Method.ToString().ToLowerInvariant(), Gender.ToString().ToUpperInvariant(), Text);
        }
    }
}