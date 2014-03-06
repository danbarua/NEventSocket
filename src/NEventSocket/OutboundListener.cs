namespace NEventSocket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reactive;

    using Common.Logging;

    /// <summary>
    ///     Listens for Outbound connections from FreeSwitch
    /// </summary>
    public class OutboundListener : IDisposable
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private bool disposed;
        private IDisposable subscription;
        private TcpListener tcpListener;
        private readonly Subject<Unit> listenerTermination = new Subject<Unit>();
        private readonly List<OutboundSocket> connections = new List<OutboundSocket>();
        private readonly Subject<OutboundSocket> observable = new Subject<OutboundSocket>();
        private readonly int port;

        /// <summary>
        ///     Starts the Listener on the given port
        /// </summary>
        /// <param name="port"></param>
        public OutboundListener(int port)
        {
            this.port = port;
        }

        ~OutboundListener()
        {
            Dispose(false);
        }


        /// <summary>
        ///     Observable of all outbound connections
        /// </summary>
        public IObservable<OutboundSocket> Connections
        {
            get { return this.observable; }
        }

        /// <summary>
        ///     Starts the Listener
        /// </summary>
        public void Start()
        {
            if (this.disposed)
                throw new ObjectDisposedException(this.ToString());

            tcpListener = TcpListener.Create(this.port);

            tcpListener.Start();

            Log.TraceFormat("OutboundListener Started on Port {0}", this.port);

            subscription =
                Observable.FromAsync(this.tcpListener.AcceptTcpClientAsync)
                          .Repeat()
                          .TakeUntil(this.listenerTermination)
                          .Select(client => new OutboundSocket(client))
                          .Subscribe(
                              connection =>
                                  {
                                      Log.Trace("New Connection");
                                      this.connections.Add(connection);
                                      this.observable.OnNext(connection);

                                      connection.Disposed += (o, e) =>
                                          {
                                              Log.Trace("Connection Disposed");
                                              this.connections.Remove(connection);
                                          };
                                  },
                              ex => Log.ErrorFormat("Error handling inbound connection", ex));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    listenerTermination.OnNext(Unit.Default);
                    listenerTermination.Dispose();

                    observable.OnCompleted();
                    observable.Dispose();

                    if (subscription != null)
                    {
                        subscription.Dispose();
                        subscription = null;
                    }

                    connections.ToList().ForEach(connection => connection.Dispose());

                    if (tcpListener != null)
                    {
                        tcpListener.Stop();
                        tcpListener = null;
                    }

                    Log.Trace("OutboundListener Disposed");
                }

                disposed = true;
            }
        }
    }
}