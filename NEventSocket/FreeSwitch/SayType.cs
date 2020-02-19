// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SayType.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    /// <summary>
    /// The module to use with the Say dialplan application
    /// </summary>
    public enum SayType
    {
#pragma warning disable 1591
        Number, 

        Items, 

        Persons, 

        Messages, 

        Currency, 

        TimeMeasurement, 

        CurrentDate, 

        CurrentTime, 

        CurrentDateTime, 

        TelephoneNumber, 

        TelephoneExtension, 

        Url, 

        IpAddress, 

        EmailAddress, 

        PostalAddress, 

        AccountNumber, 

        NameSpelled, 

        NamePhonetic,

        ShortDateTime
#pragma warning restore 1591
    }
}