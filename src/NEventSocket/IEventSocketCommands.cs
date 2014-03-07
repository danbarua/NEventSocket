namespace NEventSocket
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;

    public interface IEventSocketCommands
    {
        Task<ApiResponse> Api(string command);

        Task<BackgroundJobResult> BackgroundJob(string command, string arguments = null, Guid? jobUUID = null);

        Task<EventMessage> Execute(string uuid, string application, string applicationArguments = null, int loops = 1, bool eventLock = false, bool async = false);

        Task<CommandReply> SendCommand(string command);
    }
}