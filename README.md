NEventSocket  [![NuGet Status](http://img.shields.io/nuget/v/NEventSocket.svg?style=flat)](https://www.nuget.org/packages/NEventSocket/)
============

[![Join the chat at https://gitter.im/danbarua/NEventSocket](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/danbarua/NEventSocket?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

| Windows / .NET | Linux / Mono
| --- | ---
| [![Build status](https://ci.appveyor.com/api/projects/status/0d28m5hxdd55243q/branch/master?svg=true)](https://ci.appveyor.com/project/danbarua/neventsocket/branch/master)| [![Build Status](https://travis-ci.org/danbarua/NEventSocket.svg?branch=master)](https://travis-ci.org/danbarua/NEventSocket)

NEventSocket is a FreeSwitch [event socket](https://freeswitch.org/confluence/display/FREESWITCH/mod_event_socket) client/[server](https://freeswitch.org/confluence/display/FREESWITCH/Event+Socket+Outbound) library for .Net 4.5.

Inbound Socket Client
--------------

An ```InboundSocket``` connects and authenticates to a FreeSwitch server (inbound from the point of view of FreeSwitch) and can listen for all events going on in the system and issue commands to control calls.
You can use ReactiveExtensions to filter events using LINQ queries and extension methods.
All methods are async and awaitable.

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

  //Tell FreeSwitch which events we are interested in
  await socket.SubscribeEvents(EventName.ChannelAnswer);

  //Handle events as they come in using Rx
  socket.Events.Where(x => x.EventName == EventName.ChannelAnswer)
        .Subscribe(async x =>
            {
                Console.WriteLine("Channel Answer Event " +  x.UUID);

                //we have a channel id, now we can control it
                await socket.Play(x.UUID, "misc/8000/misc-freeswitch_is_state_of_the_art.wav");
            });

  Console.WriteLine("Press [Enter] to exit.");
  Console.ReadLine();
}
```

Outbound Socket Server
---------------
An ```OutboundListener``` listens on a TCP port for socket connections (outbound from the point of view of FreeSwitch) when the FreeSwitch dialplan is setup to route calls to the EventSocket.
An ```OutboundSocket``` receives events for one particular channel, the API is the same as for an ```InboundSocket```, so you will need to pass in the channel UUID to issue commands for it.

Don't forget to use the ```async``` and ```full``` flags in your dialplan.
````async```` means that applications will not block (e.g. a bridge will block until the channel hangs up and completes the call) and ````full```` gives the socket access to the full EventSocket api (without this you will see `-ERR Unknown Command` responses)
````xml
<action application="socket" data="127.0.0.1:8084 async full"/>
````

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

      //after calling .Connect(), socket.ChannelData
      //is populated with all the headers and variables of the channel

      var uuid = socket.ChannelData.Headers[HeaderNames.UniqueId];
      Console.WriteLine("OutboundSocket connected for channel " + uuid);

      await socket.SubscribeEvents(EventName.ChannelHangup);

      socket.Events
            .Where(x => x.EventName == EventName.ChannelHangup && x.UUID == uuid)
            .Take(1)
            .Subscribe(async x => {
                  Console.WriteLine("Hangup Detected on " + x.UUID);
                  await socket.Exit();
            });
      
      
      //if we use 'full' in our FS dialplan, we'll get events for ALL channels in FreeSwitch
      //this is not desirable here - so we'll filter in for our unique id only
      //cases where this is desirable is in the channel api where we want to catch other channels bridging to us
      await socket.Filter(HeaderNames.UniqueId, uuid);
      
      //tell FreeSwitch not to end the socket on hangup, we'll catch the hangup event and .Exit() ourselves
      await socket.Linger();
      
      await socket.ExecuteApplication(uuid, "answer");
      await socket.Play(uuid, "misc/8000/misc-freeswitch_is_state_of_the_art.wav");
      await socket.Hangup(uuid, HangupCause.NormalClearing);
    });

  listener.Start();

  Console.WriteLine("Press [Enter] to exit.");
  Console.ReadLine();
}
```

Error Handling
--------------
NEventSocket makes a best effort to handle errors gracefully, there is one scenario that you do need to handle in your application code. In a realtime async application, there may be a situation where we are trying to write to a socket when FreeSwitch has already hung up and disconnected the socket. In this case, NEventSocket will throw a ```TaskCanceledException``` (Note incorrect spelling of ```Cancelled```) which you can catch in order to do any clean up.

It's a good idea to wrap any ```IObservable.Subscribe(() => {})``` callbacks in a try/catch block.

```csharp
try {
  await socket.Connect();

  var uuid = socket.ChannelData.Headers[HeaderNames.UniqueId];
  Console.WriteLine("OutboundSocket connected for channel " + uuid);

  await socket.SubscribeEvents(EventName.Dtmf);

  socket.Events.Where(x => x.UUID == uuid && x.EventName == EventName.Dtmf)
        .Subscribe(async e => {
          try {
            Console.WriteLine(e.Headers[HeaderNames.DtmfDigit]);
           //speak the number to the caller
            await client.Say(
                  uuid,
                  new SayOptions()
                  {
                    Text = e.Headers[HeaderNames.DtmfDigit],
                    Type = SayType.Number,
                    Method = SayMethod.Iterated
                    });
           }
           catch(TaskCanceledException ex){
            //channel hungup
           }
      ));
}
catch (TaskCanceledException ex) {
  //FreeSwitch disconnected, do any clean up here.
}

```

Channel API
---------------
Whilst the ```InboundSocket``` and ```OutboundSocket``` give you a close-to-the-metal experience with the EventSocket interface, the Channel API is a high level abstraction built on top of these.
A Channel object maintains its own state by subscribing to events from FreeSwitch and allows us to control calls in a more object oriented manner without having to pass channel UUIDs around as strings.

Although the InboundSocket and OutboundSocket APIs are reasonably stable, the Channel API is a work in progress  with the goal of providing a pleasant, easy to use, strongly-typed API on top of the EventSocket.

There is an in-depth example in the examples/Channels folder.

```csharp
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

using NEventSocket;
using NEventSocket.Channels;
using NEventSocket.FreeSwitch;
using NEventSocket.Util;

using (var listener = new OutboundListener(8084))
{
  listener.Channels.Subscribe(
    async channel => {
      try
      {
          await channel.Answer();
          await channel.PlayFile("ivr/8000/ivr-call_being_transferred.wav");
          await channel.StartDetectingInbandDtmf();

          var bridgeOptions = 
                  new BridgeOptions()
                      {
                        IgnoreEarlyMedia = true,
                        RingBack =
                            "tone_stream://%(400,200,400,450);%(400,2000,400,450);loops=-1",
                        ContinueOnFail = true,
                        HangupAfterBridge = true,
                        TimeoutSeconds = 60,
                        //can get variables from a channel using the indexer
                        CallerIdName = channel["effective_caller_id_name"], 
                        CallerIdNumber = channel["effective_caller_id_number"],
                      };

          //attempt a bridge to user/1001, write to the console when it starts ringing
          await channel.BridgeTo("user/1001", 
                                  bridgeOptions,
                                  (evt) => Console.WriteLine("B-Leg is ringing..."))

          //channel.Bridge represents the bridge status
          if (!channel.Bridge.IsBridged)
          {
              //we can inspect the HangupCause to determine why it failed
              Console.WriteLine("Bridge Failed - {0}".Fmt(channel.Bridge.HangupCause));
              await channel.PlayFile("ivr/8000/ivr-call_rejected.wav");
              await channel.Hangup(HangupCause.NormalTemporaryFailure);
              return;
          }
              
          Console.WriteLine("Bridge success - {0}".Fmt(channel.Bridge.ResponseText));

          //the bridged channel maintains its own state
          //and handles a subset of full Channel operations
          channel.Bridge.Channel.HangupCallBack = 
            (e) => ColorConsole.WriteLine("Hangup Detected on B-Leg {0} {1}"
                  .Fmt(e.Headers[HeaderNames.CallerUniqueId],
                    e.Headers[HeaderNames.HangupCause]));

          //we'll listen out for the feature code #9
          //on the b-leg to do an attended transfer
          channel.Bridge.Channel.FeatureCodes("#")
            .Subscribe(async x =>
            {
              switch (x)
              {
                case "#9":
                  Console.WriteLine("Getting the extension to do an attended transfer to...");

                  //play a dial tone to the b-leg and get 4 digits to dial
                  var digits = await channel.Bridge.Channel.Read(
                                    new ReadOptions {
                                        MinDigits = 3,
                                        MaxDigits = 4, 
                                        Prompt = "tone_stream://%(10000,0,350,440)",
                                        TimeoutMs = 30000,
                                        Terminators = "#" });

                  if (digits.Result == ReadResultStatus.Success && digits.Digits.Length == 4)
                  {
                    await channel.Bridge.Channel
                      .PlayFile("ivr/8000/ivr-please_hold_while_party_contacted.wav");
                    
                    var xfer = await channel.Bridge.Channel
                      .AttendedTransfer("user/{0}".Fmt(digits));

                    //attended transfers are a work-in-progress at the moment
                    if (xfer.Status == AttendedTransferResultStatus.Failed)
                    {
                      if (xfer.HangupCause == HangupCause.CallRejected)
                      {
                          //we can play audio into the b-leg via the a-leg channel
                          await channel
                            .PlayFile("ivr/8000/ivr-call-rejected.wav", Leg.BLeg);
                      }
                      else if (xfer.HangupCause == HangupCause.NoUserResponse 
                                || xfer.HangupCause == HangupCause.NoAnswer)
                      {
                          //or we can play audio on the b-leg channel object
                          await channel.Bridge.Channel
                            .PlayFile("ivr/8000/ivr-no_user_response.wav");
                      }
                      else if (xfer.HangupCause == HangupCause.UserBusy)
                      {
                          await channel.Bridge.Channel
                            .PlayFile("ivr/8000/ivr-user_busy.wav");
                      }
                    }
                    else
                    {
                      //otherwise the a-leg is now connected to either
                      // 1) the c-leg
                      //    in this case, channel.Bridge.Channel is now the c-leg channel
                      // 2) the b-leg and the c-leg in a 3-way chat
                      //    in this case, if the b-leg hangs up, then channel.Bridge.Channel
                      //    will become the c-leg
                      await channel
                      .PlayFile("ivr/8000/ivr-call_being_transferred.wav", Leg.ALeg);
                    }
                  }
                break;
              }
            });
      }
      catch(TaskCanceledException)
      {
          Console.WriteLine("Channel {0} hungup".Fmt(channel.UUID));
      }
    }
    });

  listener.Start();

  Console.WriteLine("Press [Enter] to exit.");
  Console.ReadLine();
}
```
