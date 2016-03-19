Release Notes  [![NuGet Status](http://img.shields.io/nuget/v/NEventSocket.svg?style=flat)](https://www.nuget.org/packages/NEventSocket/)
============
1.0.2 - Bugfix: Handle multi-arg applications on Originate
                      Surface Api Response and Command Reply errors in logs

1.0.1 - Bugfix - ensure subscribed to BackgroundJob events when initiating BgAPI

1.0.0 - Remove event autosubscription [Issue #23](https://github.com/danbarua/NEventSocket/issues/23)

0.6.4 - Channels: expose Channel.Advanced.LastEvent property

0.6.3 - Bugfix - Replace .OnErrorResumeNext() with .Retry() on Channel connection error

0.6.2 - Bugfix - don't terminate OutboundListener.Channels observable on connection errors.

0.6.1 - Added events and helpers for conferences

0.6.0 - Channels: move channel variables, event headers, underlying socket to Channel.Advanced.

0.5.3 - Channels: fix channel init bug

0.5.2 - Channels: Filter events so underlying socket does not receive events for all channels

0.5.1 - Fix issues with initializing Rx when run in scriptcs

0.5.0 - Fix uri decoding issue on message parsing
Allow operations on Channels in Pre-Answer state