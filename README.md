NEventSocket
============

| Windows / .NET | Linux / Mono
| --- | ---
| [![Build status](https://ci.appveyor.com/api/projects/status/0d28m5hxdd55243q/branch/master?svg=true)](https://ci.appveyor.com/project/danbarua/neventsocket/branch/master)| [![Build Status](https://travis-ci.org/danbarua/NEventSocket.svg?branch=master)](https://travis-ci.org/danbarua/NEventSocket)

NEventSocket is a FreeSwitch [event socket](https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket) client/[server](https://freeswitch.org/confluence/display/FREESWITCH/Event+Socket+Outbound) library for .Net 4.5.

Inbound Socket Client
--------------
```csharp
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

using NEventSocket;
using NEventSocket.FreeSwitch;

using (var socket = await InboundSocket.Connect("localhost", 8021, "ClueCon"))
{
  var apiResponse = await socket.SendApi("status");
  Console.WriteLine(apiResponse.BodyText);

  socket.Events.Where(x => x.EventName == EventName.ChannelAnswer)
              .Subscribe(x =>
                  {
                      Console.WriteLine("Channel Answer Event " +  x.UUID);
                  });

  Console.WriteLine("Press [Enter] to exit.");
  Console.ReadLine();
}
```

Outbound Socket Server
---------------

```csharp
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

using NEventSocket;
using NEventSocket.FreeSwitch;

using (var listener = new OutboundListener(8084))
{
  listener.Connections.Subscribe(
    async socket => {
      await socket.Connect();

      var uuid = socket.ChannelData.Headers[HeaderNames.UniqueId];
      Console.WriteLine("OutboundSocket connected for channel " + uuid);

      socket.Events.Where(x => x.EventName == EventName.ChannelHangup)
                    .Take(1)
                    .Subscribe(x => {
                          Console.WriteLine("Hangup Detected on " + x.UUID);
                          socket.Exit();
                      });

      await socket.Linger();
      await socket.ExecuteApplication(uuid, "answer");
      await socket.Play(uuid, "misc/8000/misc-freeswitch_is_state_of_the_art.wav");
      await socket.Hangup(HangupCause.NormalClearing);
    });

  listener.Start();

  Console.WriteLine("Press [Enter] to exit.");
  Console.ReadLine();
}
```
