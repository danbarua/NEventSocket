v1.0.0
 - If you want to subscribe to events via the `socket.Events` observable, when calling ```socket.SubscribeEvents()``` you must specify which events you want to subscribe to.
 - Channels will continue to manage their own event subscriptions under the hood. Events can be subscribed via ```channel.Advanced.Socket.SubscribeEvents()```.
 - An `EventSocket` will lazily subscribe to events as needed for an `originate`, `bgapi` or dialplan application so there is no need to call `socket.SubscribeEvents()` for these operations
 - If in doubt, check the examples

 - Inbound Socket connections throw InboundSocketConnectionFailedException in expected failure scenarios