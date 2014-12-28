// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservableExtensions.cs" company="Dan Barua">
//   Copyright © Business Systems (UK) Ltd and contributors. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Util
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

    /// <summary>The observable extensions.</summary>
    public static class ObservableExtensions
    {
        /// <summary>Aggregates a Stream using the supplied Aggregator until the given predicate is true</summary>
        /// <param name="source">The source.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="accumulator">The accumulator.</param>
        /// <param name="predicate">A predicate which indicates whether the aggregation is completed.</param>
        /// <typeparam name="TSource">The Type of the Source stream.</typeparam>
        /// <typeparam name="TAccumulate">The Type of the Accumulator.</typeparam>
        /// <returns>The <see cref="IObservable{T}"/>.</returns>
        public static IObservable<TAccumulate> AggregateUntil<TSource, TAccumulate>(
            this IObservable<TSource> source, 
            Func<TAccumulate> seed, 
            Func<TAccumulate, TSource, TAccumulate> accumulator, 
            Func<TAccumulate, bool> predicate)
        {
            return Observable.Create<TAccumulate>(observer =>
            {
                var accumulate = seed();

                return source.Subscribe(
                    value =>
                        {
                            accumulate = accumulator(accumulate, value);

                                if (predicate(accumulate))
                                {
                                    observer.OnNext(accumulate);
                                    accumulate = seed();
                                }
                        },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        /// <summary>The buffer until.</summary>
        /// <param name="source">The source.</param>
        /// <param name="other">The other.</param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TOther"></typeparam>
        /// <returns>The <see cref="IObservable{T}"/>.</returns>
        public static IObservable<IList<TSource>> BufferUntil<TSource, TOther>(
            this IObservable<TSource> source, IObservable<TOther> other)
        {
            return Observable.Defer<IList<TSource>>(
                () =>
                    {
                        var list = new List<TSource>();

                        return from completeSignal in source
                                       .TakeUntil(other)
                                       .Do(value =>
                                            {
                                                lock (list)
                                                {
                                                    list.Add(value);
                                                }
                                            })
                                    .Select(_ => false)
                                    .Concat(Observable.Return(true))
                               where completeSignal
                               select list.AsReadOnly();
                    });
        }

        /// <summary>The buffer until.</summary>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The <see cref="IObservable"/>.</returns>
        public static IObservable<IEnumerable<T>> BufferUntil<T>(
            this IObservable<T> source, Func<IEnumerable<T>, bool> predicate)
        {
            return Observable.Create<IEnumerable<T>>(
                o =>
                    {
                        var buffer = new List<T>();
                        return source.Subscribe(
                            n =>
                                {
                                    buffer.Add(n);
                                    if (predicate(buffer))
                                    {
                                        o.OnNext(buffer);
                                        buffer = new List<T>();
                                    }
                                }, 
                            o.OnError, 
                            o.OnCompleted);
                    });
        }
    }
}