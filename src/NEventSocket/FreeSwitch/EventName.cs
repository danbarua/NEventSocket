// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EventName.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    public enum EventName
    {
        Custom, 

        Clone, 

        ChannelCreate, 

        ChannelDestroy, 

        ChannelState, 

        ChannelCallstate, 

        ChannelAnswer, 

        ChannelHangup, 

        ChannelHangupComplete, 

        ChannelExecute, 

        ChannelExecuteComplete, 

        ChannelHold, 

        ChannelUnhold, 

        ChannelBridge, 

        ChannelUnbridge, 

        ChannelProgress, 

        ChannelProgressMedia, 

        ChannelOutgoing, 

        ChannelPark, 

        ChannelUnpark, 

        ChannelApplication, 

        ChannelOriginate, 

        ChannelUuid, 

        Api, 

        Log, 

        InboundChan, 

        OutboundChan, 

        Startup, 

        Shutdown, 

        Publish, 

        Unpublish, 

        Talk, 

        Notalk, 

        SessionCrash, 

        ModuleLoad, 

        ModuleUnload, 

        Dtmf, 

        Message, 

        PresenceIn, 

        NotifyIn, 

        PresenceOut, 

        PresenceProbe, 

        MessageWaiting, 

        MessageQuery, 

        Roster, 

        Codec, 

        BackgroundJob, 

        DetectedSpeech, 

        DetectedTone, 

        PrivateCommand, 

        Heartbeat, 

        Trap, 

        AddSchedule, 

        DelSchedule, 

        ExeSchedule, 

        ReSchedule, 

        Reloadxml, 

        Notify, 

        SendMessage, 

        RecvMessage, 

        RequestParams, 

        ChannelData, 

        General, 

        Command, 

        SessionHeartbeat, 

        ClientDisconnected, 

        ServerDisconnected, 

        SendInfo, 

        RecvInfo, 

        RecvRtcpMessage, 

        CallSecure, 

        Nat, 

        RecordStart, 

        RecordStop, 

        PlaybackStart, 

        PlaybackStop, 

        CallUpdate, 

        Failure, 

        SocketData, 

        MediaBugStart, 

        MediaBugStop, 

        ConferenceDataQuery, 

        ConferenceData, 

        CallSetupReq, 

        CallSetupResult, 

        All, 
    }
}