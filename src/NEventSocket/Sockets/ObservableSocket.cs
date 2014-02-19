namespace NEventSocket.Sockets
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Threading;
    using System.Threading.Tasks;

    using Common.Logging;

    public class ObservableSocket : IDisposable
    {
        protected bool disposed;

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly BlockingCollection<byte[]> received = new BlockingCollection<byte[]>(1024 * 1024);

        private readonly IObservable<byte[]> receiver;

        private readonly Subject<Unit> receiverTermination = new Subject<Unit>();

        private readonly object syncLock = new object();

        protected TcpClient tcpClient;

        private IDisposable readSubscription;

        protected ObservableSocket(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;

            this.receiver = this.received.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .TakeUntil(this.receiverTermination);

            this.readSubscription = Observable.Defer(
                () =>
                    {
                        var stream = tcpClient.GetStream();
                        var buffer = new byte[8192];
                        return
                            Observable.FromAsync(() => stream.ReadAsync(buffer, 0, buffer.Length))
                                      .Select(x => buffer.Take(x).ToArray());
                    })
                    .Repeat()
                    .TakeWhile(x => x.Any())
                    .Subscribe(
                        bytes => this.received.Add(bytes),
                        ex =>
                            {
                                Log.Error("Read Failed", ex);
                                this.Disconnect(false);
                            },
                        () =>
                            {
                                Log.Trace("Read Observable Completed");
                                this.Disconnect(false);
                            });
        }

        ~ObservableSocket()
        {
            this.Dispose(false);
        }

        public event EventHandler Disconnected = (sender, args) => { };

        public event EventHandler Disposed = (sender, args) => { };

        public bool IsConnected { get { return this.tcpClient != null && this.tcpClient.Connected; } }

        protected IObservable<byte[]> Receiver { get { return this.receiver; } }

        public Task SendAsync(byte[] bytes)
        {
            return this.SendAsync(bytes, CancellationToken.None);
        }

        public Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (this.disposed) throw new ObjectDisposedException(this.ToString());
            
            if (!this.IsConnected) throw new InvalidOperationException("Not connected");
            
            try
            {
                Monitor.Enter(this.syncLock);
                var stream = this.GetStream();
                return stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error("Error writing.", ex);
                this.Disconnect();
                throw;
            }
            finally
            {
                Monitor.Exit(this.syncLock);
            }
        }

        public void Disconnect()
        {
            if (!this.IsConnected)
                throw new InvalidOperationException("Client is not connected");

            this.Disconnect(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual Stream GetStream()
        {
            return this.tcpClient.GetStream();
        }

        protected void Disconnect(bool disposing)
        {
            if (this.disposed && !disposing)
                throw new ObjectDisposedException(this.ToString());


            if (this.readSubscription != null)
            {
                this.readSubscription.Dispose();
            }

            this.readSubscription = null;

            if (this.IsConnected)
            {
                Log.Debug("Disconnecting");
                this.tcpClient.Close();
                this.tcpClient = null;
                Log.Debug("Client closed.");

                this.Disconnected(this, EventArgs.Empty);
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            Log.Debug("Disposing");

            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Disconnect(true);

                    this.receiverTermination.OnNext(Unit.Default);
                }

                this.Disposed(this, EventArgs.Empty);

                this.disposed = true;
            }
        }
    }
}