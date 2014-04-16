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
            Log = LogManager.GetLogger(this.GetType());

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
                    .Do((bytes) => received.Add(bytes))
                    .Subscribe(
                        _ => { },
                        ex =>
                            {
                                Log.Error("Read Failed", ex);
                                Dispose();
                            },
                        () =>
                            {
                                Log.Trace("Read Observable Completed");
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

        //public Task SendAsync(byte[] bytes)
        //{
        //    return SendAsync(bytes, CancellationToken.None);
        //}

        /// <summary>
        /// Asynchronously writes the given bytes to the socket.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
                    Log.Warn("Socket disconnected");
                    this.Dispose();
                    return;
                }

                throw;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    Log.Warn("Socket disconnected");
                    this.Dispose();
                    return;
                }

                throw;
            }
            catch (Exception ex)
            {
                Log.Error("Error writing.", ex);
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
                Log.Trace("Disposing");
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
                        receiverTermination.Dispose();
                        receiverTermination = null;
                    }

                    if (received != null)
                    {
                        received.Dispose();
                        received = null;
                    }
                }

                if (IsConnected)
                {
                    if (tcpClient != null)
                    {
                        tcpClient.Close();
                        tcpClient = null;
                        Log.Trace("Client closed.");
                    }
                }
                
                disposed = true;

                Disposed(this, EventArgs.Empty);
            }
        }
    }
}