// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Rx.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket
{
    using System.Reactive.PlatformServices;

    internal static class Rx
    {
        static Rx()
        {
            //needed to work around issues ILMerging Reactive Extensions
            PlatformEnlightenmentProvider.Current = new CurrentPlatformEnlightenmentProvider();
        }
    }
}