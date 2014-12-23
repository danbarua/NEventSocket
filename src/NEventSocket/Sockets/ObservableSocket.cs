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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using NEventSocket.Logging;

    using NEventSocket.Util;

    public abstract class ObservableSocket : IDisposable
    {
        protected bool disposed;

        protected TcpClient tcpClient;

        private readonly ILog Log;

        private readonly SemaphoreSlim syncLock = new SemaphoreSlim(1);

        private readonly IObservable<byte[]> receiver;

        private Subject<Unit> receiverTermination = new Subject<Unit>();

        private IDisposable readSubscription;

        private BlockingCollection<byte[]> received = new BlockingCollection<byte[]>(1024 * 1024);

        protected ObservableSocket(TcpClient tcpClient)
        {
            Log = LogProvider.GetLogger(this.GetType());

            this.tcpClient = tcpClient;

            receiver = received.GetConsumingEnumerable()
                .ToObservable(Scheduler.Default)
                .TakeUntil(receiverTermination);

            readSubscription = Observable.Defer(
                () =>
                    {
                        var stream = tcpClient.GetStream();
                        var buffer = new byte[8192]; //todo: use bufferpool or socketasynceventargs
                        return
                            Observable.FromAsync(() => stream.ReadAsync(buffer, 0, buffer.Length))
                                      .Select(x => buffer.Take(x).ToArray());
                    })
                    .Repeat()
                    .TakeWhile(x => x.Any())
                    //.Do(bytes => Log.Trace(() => "Bytes received: {0}".Fmt(Encoding.ASCII.GetString(bytes))))
                    .Subscribe(
                        (bytes) => received.Add(bytes),
                        ex =>
                            {
                                Log.ErrorException("Read Failed", ex);
                                Dispose();
                            },
                        () =>
                            {
                                Log.Trace(() => "Read Observable Completed");
                                Dispose();
                            });
        }

        ~ObservableSocket()
        {
            Dispose(false);
        }

        public event EventHandler Disposed = (sender, args) => { };

        public bool IsConnected { get { return tcpClient != null && tcpClient.Connected; } }

        protected IObservable<byte[]> Receiver { get { return receiver; } }

        /// <summary>
        /// Asynchronously writes the given message to the socket.
        /// </summary>
        /// <param name="message">The string message to send</param>
        /// <param name="cancellationToken">A CancellationToken to cancel the send operation.</param>
        /// <returns>A Task.</returns>
        /// <exception cref="ObjectDisposedException">If disposed.</exception>
        /// <exception cref="InvalidOperationException">If not connected.</exception>
        public Task SendAsync(string message, CancellationToken cancellationToken)
        {
            return SendAsync(Encoding.ASCII.GetBytes(message), cancellationToken);
        }

        /// <summary>
        /// Asynchronously writes the given bytes to the socket.
        /// </summary>
        /// <param name="bytes">The raw byts to stream through the socket.</param>
        /// <param name="cancellationToken">A CancellationToken to cancel the send operation.</param>
        /// <returns>A Task.</returns>
        /// <exception cref="ObjectDisposedException">If disposed.</exception>
        /// <exception cref="InvalidOperationException">If not connected.</exception>
        public async Task SendAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (disposed) throw new ObjectDisposedException(ToString());
            
            if (!IsConnected) throw new InvalidOperationException("Not connected");

            try
            {
                await syncLock.WaitAsync();
                var stream = GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException
                    && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionAborted)
                {
                    Log.Warn(() => "Socket disconnected");
                    this.Dispose();
                    return;
                }

                throw;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    Log.Warn(() => "Socket disconnected");
                    this.Dispose();
                    return;
                }

                throw;
            }
            catch (Exception ex)
            {
                Log.ErrorException("Error writing", ex);
                Dispose();
                throw;
            }
            finally
            {
                syncLock.Release();
            }
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Log.Trace(() => "Disposing (disposing:{0})".Fmt(disposing));
                if (disposing)
                {
                    if (readSubscription != null)
                    {
                        readSubscription.Dispose();
                        readSubscription = null;
                    }

                    if (receiverTermination != null)
                    {
                        receiverTermination.OnNext(Unit.Default);

                        if (receiverTermination != null)
                        {
                            receiverTermination.Dispose();
                            receiverTermination = null;
                        }
                    }

                    if (received != null)
                    {
                        received.Dispose();
                        received = null;
                    }

                    if (IsConnected)
                    {
                        if (tcpClient != null)
                        {
                            tcpClient.Close();
                            tcpClient = null;
                            Log.Trace(() => "TcpClient closed");
                        }
                    }
                }
                
                disposed = true;

                Disposed(this, EventArgs.Empty);

                Log.Trace(() => "Disposed");
            }
        }
    }
}