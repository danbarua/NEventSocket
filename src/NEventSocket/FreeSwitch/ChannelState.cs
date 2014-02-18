namespace NEventSocket.FreeSwitch
{
    // ReSharper disable InconsistentNaming
    public enum ChannelState
    {
        CS_NEW, // Channel is newly created 
        CS_INIT, // Channel has been initialized
        CS_ROUTING, // Channel is looking for an extension to execute
        CS_SOFT_EXECUTE, // Channel is ready to execute from 3rd party control
        CS_EXECUTE, // Channel is executing it's dialplan 
        CS_EXCHANGE_MEDIA, // Channel is exchanging media with another channel.
        CS_PARK, // Channel is accepting media awaiting commands.
        CS_CONSUME_MEDIA, // Channel is consuming all media and dropping it.
        CS_HIBERNATE, // Channel is in a sleep state
        CS_RESET, // Channel is in a reset state
        CS_HANGUP, // Channel is flagged for hangup and ready to end
        CS_DONE, // Channel is ready to be destroyed and out of the state machine
        CS_DESTROY // Channel is ready to be destroyed and out of the state machine (DUPLICATE?)
    }
    // ReSharper restore InconsistentNaming
}