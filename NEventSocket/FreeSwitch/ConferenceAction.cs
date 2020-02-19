// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConferenceAction.cs" company="Dan Barua">
//   (C) Dan Barua and contributors. Licensed under the Mozilla Public License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NEventSocket.FreeSwitch
{
    //https://freeswitch.org/stash/projects/FS/repos/freeswitch/browse/src/mod/applications/mod_conference/mod_conference.c
    public enum ConferenceAction
    {
        AddMember,
        DelMember,
        VideoFloorChange,
        FloorChange,
        PlayFile,
        PlayFileDone,
        PlayFileMember,
        PlayFileMemberDone,
        SpeakText,
        SpeakTextMember,
        ConferenceCreate,
        ConferenceDestroy,
        Lock,
        Unlock,
        EnergyLevel,
        VolumeLevel,
        GainLevel,
        Dtmf,
        DtmfMember,
        Transfer,
        ExecuteApp, //execute_app not execute-app
        StopTalking,
        StartTalking,
        MuteDetect,
        StartRecording,
        StopRecording,
        PauseRecording,
        ResumeRecording,
        MuteMember,
        UnmuteMember,
        VmuteMember,
        UnvmuteMember,
        DeafMember,
        UndeafMember,
        HupMember,
        KickMember,
        EnergyLevelMember,
        SetPositionMember,
        VolumeInMember,
        VolumeOutMember,
        ExitSoundsOn,
        ExitSoundsOff,
        ExitSoundFileChanged,
        EnterSoundsOn,
        EnterSoundsOff,
        EnterSoundFileChanged,
        BgDialResult,
        RejectedJoinOnly,
        Requestcontrols,
    }
}