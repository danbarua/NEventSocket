// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventSocket.cs" company="Business Systems (UK) Ltd">
//   (C) Business Systems (UK) Ltd
// </copyright>
// <summary>
//   Base Class for an EventSocket connection to FreeSwitch
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace NEventSocket.Sockets.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.Messages;
    using NEventSocket.Sockets.Interfaces;
    using NEventSocket.Sockets.Protocol;
    using NEventSocket.Util;

    /// <summary>
    ///     Base Class for an EventSocket connection to FreeSwitch
    /// </summary>
    public abstract class OldEventSocket : Commands, IEventSocket
    {
        /// <summary>The incoming messages.</summary>
        protected IConnectableObservable<BasicMessage> IncomingMessages;

        /// <summary>The tcp client.</summary>
        protected TcpClient TcpClient;

        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // private readonly Queue<ICallBack> commandCallbacks = new Queue<ICallBack>();
         
        private readonly Queue<Action<BasicMessage>> callbacks = new Queue<Action<BasicMessage>>(); 

        private readonly IDisposable incomingMessagesPublisher;

        private readonly ReaderWriterLockSlim syncLock = new ReaderWriterLockSlim();

        private bool disposed;

        /// <summary>Initialises a new instance of the <see cref="EventSocket"/> class.</summary>
        /// <param name="tcpClient">The tcp client.</param>
        protected OldEventSocket(TcpClient tcpClient)
        {
            this.TcpClient = tcpClient;

            this.IncomingMessages =
                tcpClient.Client.ReceiveUntilCompleted(SocketFlags.None)
                         .ExtractBasicMessages()
                         .SubscribeOn(TaskPoolScheduler.Default)
                         .Multicast(new Subject<BasicMessage>());

            // we can use this to kill off all subscribers when we dispose the socket
            this.incomingMessagesPublisher = this.IncomingMessages.Connect();

            // handle command responses
            this.IncomingMessages.Where(
                x => x.ContentType == ContentTypes.CommandReply || x.ContentType == ContentTypes.ApiResponse)
                .Subscribe(
                    response =>
                        {
                            // var response = new CommandReply(x);
                            Log.DebugFormat("Command Response Received\r\n{0}", response);

                            this.syncLock.EnterReadLock();
                            try
                            {
                                var callback = this.callbacks.Dequeue();
                                callback(response);
                            }
                            finally
                            {
                                this.syncLock.ExitUpgradeableReadLock();
                            }
                        });

            Log.Debug("EventSocket initialized");
            this.Connected(this, EventArgs.Empty);
        }

        /// <summary>The connected.</summary>
        public event EventHandler Connected = (sender, args) => { };

        /// <summary>The disconnected.</summary>
        public event EventHandler Disconnected = (sender, args) => { };

        /// <summary>The disposed.</summary>
        public event EventHandler Disposed = (sender, args) => { };

        /// <summary>
        ///     Gets whether the socket is connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.TcpClient != null && this.TcpClient.Connected;
            }
        }

        /// <summary>
        ///     Observable of all Messages received on this connection
        /// </summary>
        public IObservable<BasicMessage> MessagesReceived
        {
            get
            {
                return this.IncomingMessages;
            }
        }

        /// <summary>
        ///     Observable of all Events received on this connection
        /// </summary>
        public IObservable<EventMessage> EventsReceived
        {
            get
            {
                return
                    this.IncomingMessages.Where(x => x.ContentType == ContentTypes.EventPlain)
                        .Select(x => new EventMessage(x));
            }
        }

        /// <summary>The bg api.</summary>
        /// <param name="command">The command.</param>
        /// <param name="arg">The arg.</param>
        /// <param name="jobUUID">The job uuid.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public Task<BackgroundJobResult> BgApi(string command, string arg = null, Guid? jobUUID = null)
        {
            if (jobUUID == null) jobUUID = Guid.NewGuid();

            var tcs = new TaskCompletionSource<BackgroundJobResult>();

            // we'll get an event in the future for this JobUUID and we'll use that to complete the task
            var subscription = this.EventsReceived.Where(
                x => x.EventType == EventType.BACKGROUND_JOB && x.EventHeaders["Job-UUID"] == jobUUID.ToString())
                                   .Take(1) // will auto terminate the subscription
                                   .Subscribe(x => tcs.SetResult(new BackgroundJobResult(x)));

            this.SendCommandAsync(
                arg != null
                    ? "bgapi {0} {1}\nJob-UUID: {2}".Fmt(command, arg, jobUUID)
                    : "bgapi {0}\nJob-UUID: {1}".Fmt(command, jobUUID)).ContinueWith(
                        t =>
                            {
                                if (t.IsFaulted && t.Exception != null)
                                {
                                    // we're never going to get a BACKGROUND_JOB event because we didn't send the command successfully
                                    subscription.Dispose();

                                    // fail the parent task
                                    tcs.SetException(t.Exception);
                                }
                            });

            return tcs.Task;
        }

        /// <summary>The disconnect.</summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Disconnect()
        {
            if (!this.IsConnected) throw new InvalidOperationException("Attempted to disconnect a client which was not connected");

            this.Disconnect(false);
        }

        /// <summary>The dispose.</summary>
        public virtual void Dispose()
        {
            if (this.disposed) return;

            this.disposed = true;
            this.Disconnect(true);

            this.Disposed(this, EventArgs.Empty);
        }

        /// <summary>The send expect response.</summary>
        /// <param name="command">The command.</param>
        /// <param name="responseMutator">The response mutator.</param>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns>The <see cref="Task"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected override Task<TResponse> SendExpectResponse<TResponse>(
            string command, Func<BasicMessage, TResponse> responseMutator)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (responseMutator == null) throw new ArgumentNullException("responseMutator");
            return this.SendExpectResponse(command, responseMutator, this.cancellationTokenSource.Token);
        }

        /// <summary>The send expect response.</summary>
        /// <param name="command">The command.</param>
        /// <param name="responseMutator">The response mutator.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TResponse"></typeparam>
        /// <returns>The <see cref="Task"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        protected Task<TResponse> SendExpectResponse<TResponse>(
            string command, Func<BasicMessage, TResponse> responseMutator, CancellationToken cancellationToken)
            where TResponse : BasicMessage
        {
            if (command == null) throw new ArgumentNullException("command");
            if (responseMutator == null) throw new ArgumentNullException("responseMutator");

            if (this.disposed) throw new ObjectDisposedException(this.ToString());

            if (!this.IsConnected) throw new InvalidOperationException("Not connected");

            var tcs = new TaskCompletionSource<TResponse>();

            this.syncLock.EnterWriteLock();
            try
            {
                Log.DebugFormat("Sending Command '{0}'", command);
                var bytes = Encoding.ASCII.GetBytes(command + "\n\n");
                var stream = this.TcpClient.GetStream();

                Log.DebugFormat("Dispatching command {1} on Thread {0}", Thread.CurrentThread.ManagedThreadId, command);

                Task.Factory.FromAsync(
                    stream.BeginWrite, 
                    stream.EndWrite, 
                    bytes, 
                    0, 
                    bytes.Length, 
                    null, 
                    TaskCreationOptions.AttachedToParent)
                    .ContinueWith(
                        t =>
                            {
                                if (!t.IsFaulted && !t.IsCanceled)
                                    this.callbacks.Enqueue((msg) => tcs.SetResult(msg));
                            })
                    .Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                if (this.IsConnected) this.Disconnect();
                tcs.SetException(ex);
            }
            finally
            {
                this.syncLock.ExitWriteLock();
            }

            return tcs.Task;
        }

        /// <summary>The disconnect.</summary>
        /// <param name="disposing">The disposing.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void Disconnect(bool disposing)
        {
            if (this.disposed && !disposing) throw new ObjectDisposedException(this.ToString());

            // cancel any outgoing network sends
            this.cancellationTokenSource.Cancel();

            // cancel any subscriptions to the incoming messages stream
            this.incomingMessagesPublisher.Dispose();

            if (this.IsConnected)
            {
                try
                {
                    this.TcpClient.Close();
                }
                catch (SocketException ex)
                {
                    Log.Warn("SocketException when trying to close TcpClient", ex);
                }
            }

            this.TcpClient = null;

            this.Disconnected(this, EventArgs.Empty);
        }
    }
}