// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ObservableSocket.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
    using NEventSocket.Util.ObjectPooling;

    /// <summary>
    /// Wraps a <seealso cref="TcpClient"/> exposing incoming strings as an Observable sequence.
    /// </summary>
    public abstract class ObservableSocket : IDisposable
    {
        private bool disposed;

        private readonly ILog Log;

        private readonly SemaphoreSlim syncLock = new SemaphoreSlim(1);

        private readonly IObservable<byte[]> receiver;

        private TcpClient tcpClient;

        private Subject<Unit> receiverTermination = new Subject<Unit>();

        private IDisposable readSubscription;

        private BlockingCollection<byte[]> received = new BlockingCollection<byte[]>(16);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSocket"/> class.
        /// </summary>
        /// <param name="tcpClient">The TCP client to wrap.</param>
        protected ObservableSocket(TcpClient tcpClient)
        {
            Log = LogProvider.GetLogger(this.GetType());

            this.tcpClient = tcpClient;

            receiver = received.GetConsumingEnumerable().ToObservable(Scheduler.Default).TakeUntil(receiverTermination);

            readSubscription = Observable.Defer(
                () =>
                    {
                        var stream = tcpClient.GetStream();
                        var buffer = SharedPools.ByteArray.Allocate(); // new byte[8192]; //todo: use bufferpool or socketasynceventargs
                        return
                            Observable.FromAsync(() => stream.ReadAsync(buffer, 0, buffer.Length))
                                      .Select(x => buffer.Take(x).ToArray())
                                      .Do(_ => SharedPools.ByteArray.Free(buffer));
                    }).Repeat().TakeWhile(x => x.Any()).Subscribe(
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

        /// <summary>
        /// Finalizes an instance of the <see cref="ObservableSocket"/> class.
        /// </summary>
        ~ObservableSocket()
        {
            Dispose(false);
        }

        /// <summary>
        /// Occurs when the <see cref="ObservableSocket"/> is disposed.
        /// </summary>
        public event EventHandler Disposed = (sender, args) => { };

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get
            {
                return tcpClient != null && tcpClient.Connected;
            }
        }

        /// <summary>
        /// Gets an Observable sequence of byte array chunks as read from the socket stream.
        /// </summary>
        protected IObservable<byte[]> Receiver
        {
            get
            {
                return receiver;
            }
        }

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
            if (disposed)
            {
                throw new ObjectDisposedException(ToString());
            }

            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected");
            }

            try
            {
                await syncLock.WaitAsync();
                var stream = GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Log.Warn(() => "Write operation was cancelled.");
                this.Dispose();
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the underlying network stream.
        /// </summary>
        protected virtual Stream GetStream()
        {
            return tcpClient.GetStream();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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