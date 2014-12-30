// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservableListener.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;

    using NEventSocket.Logging;
    using NEventSocket.Util;

    /// <summary>
    /// A Reactive wrapper around a TcpListener
    /// </summary>
    /// <typeparam name="T">The type of <seealso cref="ObservableSocket"/> that this listener will provide.</typeparam>
    public abstract class ObservableListener<T> : IDisposable where T : ObservableSocket
    {
        private readonly ILog Log;

        private readonly Subject<Unit> listenerTermination = new Subject<Unit>();

        private readonly List<T> connections = new List<T>();

        private readonly Subject<T> observable = new Subject<T>();

        private readonly int port;

        private readonly Func<TcpClient, T> observableSocketFactory;

        private bool disposed;

        private IDisposable subscription;

        private TcpListener tcpListener;

        /// <summary>
        /// Starts the Listener on the given port
        /// </summary>
        /// <param name="port">The Tcp Port on which to listen for incoming connections.</param>
        /// <param name="observableSocketFactory">A function returning an object that inherits from <seealso cref="ObservableSocket" />.</param>
        protected ObservableListener(int port, Func<TcpClient, T> observableSocketFactory)
        {
            this.Log = LogProvider.GetLogger(this.GetType());
            this.port = port;
            this.observableSocketFactory = observableSocketFactory;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ObservableListener{T}"/> class.
        /// </summary>
        ~ObservableListener()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets an observable sequence of all outbound connections from FreeSwitch.
        /// </summary>
        public IObservable<T> Connections
        {
            get
            {
                return this.observable;
            }
        }

        /// <summary>
        /// Gets the Tcp Port that the Listener is waiting for connections on.
        /// </summary>
        public int Port
        {
            get
            {
                return ((IPEndPoint)this.tcpListener.LocalEndpoint).Port;
            }
        }

        /// <summary>
        /// Starts the Listener
        /// </summary>
        public void Start()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            this.tcpListener = new TcpListener(IPAddress.Any, port);

            this.tcpListener.Start();

            Log.Trace(() => "Listener Started on Port {0}".Fmt(this.Port));

            this.subscription =
                Observable.FromAsync(this.tcpListener.AcceptTcpClientAsync)
                          .Repeat()
                          .TakeUntil(this.listenerTermination)
                          .Do(connection => Log.Trace(() => "New Connection from {0}".Fmt(connection.Client.RemoteEndPoint)))
                          .Select(tcpClient => observableSocketFactory(tcpClient))
                          .Subscribe(
                              connection =>
                                  {
                                      this.connections.Add(connection);
                                      this.observable.OnNext(connection);

                                      connection.Disposed += ConnectionOnDisposed;
                                  }, 
                              ex => Log.ErrorFormat("Error handling inbound connection", ex));
        }

        /// <summary>
        /// Stops and closes down the Listener.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                Log.Trace(() => "Disposing (disposing:{0})".Fmt(disposing));

                if (disposing)
                {
                    this.listenerTermination.OnNext(Unit.Default);
                    this.listenerTermination.Dispose();

                    this.observable.OnCompleted();
                    this.observable.Dispose();

                    if (this.subscription != null)
                    {
                        this.subscription.Dispose();
                        this.subscription = null;
                    }

                    this.connections.ToList().ForEach(connection => connection.Dispose());

                    if (this.tcpListener != null)
                    {
                        this.tcpListener.Stop();
                        this.tcpListener = null;
                    }
                }

                Log.Trace(() => "Disposed");
                this.disposed = true;
            }
        }

        private void ConnectionOnDisposed(object sender, EventArgs eventArgs)
        {
            var connection = sender as T;
            if (connection != null)
            {
                Log.Trace(() => "Connection Disposed");
                connection.Disposed -= this.ConnectionOnDisposed;
                this.connections.Remove(connection);
            }
        }
    }
}