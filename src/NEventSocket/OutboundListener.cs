namespace NEventSocket
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reactive;

    using Common.Logging;

    /// <summary>
    ///     Listens for Outbound connections from FreeSwitch
    /// </summary>
    public class OutboundListener
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

            this.tcpListener = TcpListener.Create(this.port);

            this.tcpListener.Start();

            Log.TraceFormat("OutboundListener Started on Port {0}", this.port);

            this.subscription = Observable.FromAsync(this.tcpListener.AcceptTcpClientAsync)
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
                              });
        }

        /// <summary>
        ///     Disposes of the listener, stopping and disposing of all active connections
        /// </summary>
        public void Dispose()
        {
            if (this.disposed)
                return;

            this.disposed = true;

            this.listenerTermination.OnNext(Unit.Default);

            this.observable.OnCompleted();

            this.subscription.Dispose();
            this.subscription = null;
            this.tcpListener.Server.Close();
            this.tcpListener = null;


            this.connections.ToList().ForEach(connection => connection.Dispose());

            Log.Trace("OutboundListener Disposed");
        }
    }
}