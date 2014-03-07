namespace NEventSocket
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;

    public interface IEventSocketCommands
    {
        Task<ApiResponse> Api(string command);

        Task<BackgroundJobResult> BackgroundJob(string command, string arg = null, Guid? jobUUID = null);

        Task<EventMessage> Execute(string uuid, string appName, string appArg = null);

        Task<CommandReply> SendCommand(string command);
    }
}