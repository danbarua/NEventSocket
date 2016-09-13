v2.1.0
 - Split ChannelEvents from Events observable, created new ChannelEvent message type
v2.0.0
 - Moved Originate operations from InboundSocket to extension methods on EventSocket (breaks binary compatibility)
 - Channels - exposes socket and last event properties on Channel instead of Channel.Advanced
 - Changed type of BridgeOptions.FilterDtmf from boolean to Leg enum - allow one or both legs to filter DTMF
v1.0.0
 - If you want to subscribe to events via the `socket.Events` observable, when calling ```socket.SubscribeEvents()``` you must specify which events you want to subscribe to.
 - Channels will continue to manage their own event subscriptions under the hood. Events can be subscribed via ```channel.Socket.SubscribeEvents()```.
 - An `EventSocket` will lazily subscribe to events as needed for an `originate`, `bgapi` or dialplan application so there is no need to call `socket.SubscribeEvents()` for these operations
 - If in doubt, check the examples

 - Inbound Socket connections throw InboundSocketConnectionFailedException in expected failure scenarios