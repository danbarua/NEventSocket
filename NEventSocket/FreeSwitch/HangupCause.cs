﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HangupCause.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
#pragma warning disable 1591
    public enum HangupCause
    {
        None = 0, 

        UnallocatedNumber = 1, 

        NoRouteTransitNet = 2, 

        NoRouteDestination = 3, 

        ChannelUnacceptable = 6, 

        CallAwardedDelivered = 7, 

        NormalClearing = 16, 

        UserBusy = 17, 

        NoUserResponse = 18, 

        NoAnswer = 19, 

        SubscriberAbsent = 20, 

        CallRejected = 21, 

        NumberChanged = 22, 

        RedirectionToNewDestination = 23, 

        ExchangeRoutingError = 25, 

        DestinationOutOfOrder = 27, 

        InvalidNumberFormat = 28, 

        FacilityRejected = 29, 

        ResponseToStatusEnquiry = 30, 

        NormalUnspecified = 31, 

        NormalCircuitCongestion = 34, 

        NetworkOutOfOrder = 38, 

        NormalTemporaryFailure = 41, 

        SwitchCongestion = 42, 

        AccessInfoDiscarded = 43, 

        RequestedChanUnavail = 44, 

        PreEmpted = 45, 

        FacilityNotSubscribed = 50, 

        OutgoingCallBarred = 52, 

        IncomingCallBarred = 54, 

        BearercapabilityNotauth = 57, 

        BearercapabilityNotavail = 58, 

        ServiceUnavailable = 63, 

        BearercapabilityNotimpl = 65, 

        ChanNotImplemented = 66, 

        FacilityNotImplemented = 69, 

        ServiceNotImplemented = 79, 

        InvalidCallReference = 81, 

        IncompatibleDestination = 88, 

        InvalidMsgUnspecified = 95, 

        MandatoryIeMissing = 96, 

        MessageTypeNonexist = 97, 

        WrongMessage = 98, 

        IeNonexist = 99, 

        InvalidIeContents = 100, 

        WrongCallState = 101, 

        RecoveryOnTimerExpire = 102, 

        MandatoryIeLengthError = 103, 

        ProtocolError = 111, 

        Interworking = 127, 

        Success = 142, 

        OriginatorCancel = 487, 

        Crash = 500, 

        SystemShutdown = 501, 

        LoseRace = 502, 

        ManagerRequest = 503, 

        BlindTransfer = 600, 

        AttendedTransfer = 601, 

        AllottedTimeout = 602, 

        UserChallenge = 603, 

        MediaTimeout = 604, 

        PickedOff = 605, 

        UserNotRegistered = 606, 

        ProgressTimeout = 607, 

        InvalidGateway = 608,
 
        GatewayDown = 609,
 
        InvalidUrl = 610,
 
        InvalidProfile = 611,
 
        NoPickup = 612,
 
        SrtpReadError = 613,
 
        Bowout = 614,
 
        BusyEverywhere = 615,
 
        Decline = 616,
 
        DoesNotExistAnywhere = 617,
 
        NotAcceptable = 618,
 
        Unwanted = 619,
 
        NoIdentity = 620,
 
        BadIdentityInfo = 621,
 
        UnsupportedCertificate = 622,
 
        InvalidIdentity = 623,
 
        StaleDate = 624,
 
        Unknown = 9999
    }
#pragma warning restore 1591
}