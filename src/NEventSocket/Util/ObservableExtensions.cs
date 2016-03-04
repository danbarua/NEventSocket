// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservableExtensions.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Util
{
    using System;
    using System.Reactive.Linq;

    using NEventSocket.Logging;

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
            return Observable.Create<TAccumulate>(
                observer =>
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
        
        public static IObservable<TSource> Trace<TSource>(this IObservable<TSource> source, ILog log, string name)
        {
            var id = 0;
            return Observable.Create<TSource>(observer => {

                var itemId = ++id;
                Action<string, object> trace =
                    (m, v) =>
                        log.Info(
                            () => "{0}{1}: {2}({3})".Fmt(name, itemId, m, v));

                trace("Subscribe", null);
                IDisposable disposable = source.Subscribe(
                    v => { trace("OnNext", v); observer.OnNext(v); },
                    e => { trace("OnError", e); observer.OnError(e); },
                    () => { trace("OnCompleted", null); observer.OnCompleted(); });

                return () => { trace("Dispose", null); disposable.Dispose(); };
            });
        }
    }
}