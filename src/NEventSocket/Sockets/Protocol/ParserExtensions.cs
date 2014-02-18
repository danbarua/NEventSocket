// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParserExtensions.cs" company="Business Systems (UK) Ltd">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// <summary>
//   Defines the ParserExtensions type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.Sockets.Protocol
{
    using System;
    using System.Reactive.Linq;
    using System.Text;

    using NEventSocket.Messages;
    using NEventSocket.Util;

    public static class ParserExtensions
    {
        public static IObservable<BasicMessage> ExtractBasicMessages(
            this IObservable<byte[]> byteStream)
        {
            return byteStream.SelectMany(x => Encoding.ASCII.GetString(x)).ExtractBasicMessages();
        }

        public static IObservable<BasicMessage> ExtractBasicMessages(
            this IObservable<char> charStream)
        {
            return
                charStream.AggregateUntil(() => new Parser(), (builder, ch) => builder.Append(ch), builder => builder.Completed)
                          .Select(builder => builder.ParseMessage());
        }
    }
}