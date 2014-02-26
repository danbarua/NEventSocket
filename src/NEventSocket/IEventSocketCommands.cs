namespace NEventSocket
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.FreeSwitch;

    public interface IEventSocketCommands
    {
        Task<EventMessage> ExecuteAppAsync(string uuid, string appName, string appArg = null);

        Task<ApiResponse> SendApiAsync(string command);

        Task<BackgroundJobResult> BackgroundJob(string command, string arg = null, Guid? jobUUID = null);

        Task<CommandReply> SendCommandAsync(string command);
    }
}