namespace NEventSocket.Sockets.Implementation
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

            receiver = received.GetConsumingEnumerable()
                .ToObservable(TaskPoolScheduler.Default)
                .TakeUntil(receiverTermination);

            readSubscription = Observable.Defer(
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
                                Disconnect(false);
                            },
                        () => Disconnect(false));
        }

        ~ObservableSocket()
        {
            Dispose(false);
        }

        public event EventHandler Disconnected = (sender, args) => { };

        public event EventHandler Disposed = (sender, args) => { };

        public bool IsConnected { get { return tcpClient != null && tcpClient.Connected; } }

        protected IObservable<byte[]> Receiver { get { return receiver; } }

        public Task SendAsync(byte[] bytes)
        {
            return SendAsync(bytes, CancellationToken.None);
        }

        public Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (disposed) throw new ObjectDisposedException(this.ToString());
            
            if (!IsConnected) throw new InvalidOperationException("Not connected");
            
            try
            {
                Monitor.Enter(syncLock);
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
                Monitor.Exit(syncLock);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Client is not connected");

            Disconnect(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual Stream GetStream()
        {
            return tcpClient.GetStream();
        }

        protected void Disconnect(bool disposing)
        {
            if (disposed && !disposing)
                throw new ObjectDisposedException(this.ToString());

            Log.Debug("Disconnecting");

            if (readSubscription != null)
            {
                readSubscription.Dispose();
            }

            readSubscription = null;

            if (IsConnected)
            {
                tcpClient.Close();
                Log.Debug("Client closed.");
            }

            tcpClient = null;

            Disconnected(this, EventArgs.Empty);
        }

        protected virtual void Dispose(bool disposing)
        {
            Log.Debug("Disposing");

            if (!disposed)
            {
                if (disposing)
                {
                    Disconnect(true);

                    receiverTermination.OnNext(Unit.Default);
                }

                Disposed(this, EventArgs.Empty);

                disposed = true;
            }
        }
    }
}