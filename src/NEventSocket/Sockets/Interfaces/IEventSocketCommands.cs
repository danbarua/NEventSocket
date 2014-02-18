namespace NEventSocket.Sockets.Interfaces
{
    using System;
    using System.Threading.Tasks;

    using NEventSocket.Messages;

    public interface IEventSocketCommands
    {
        Task<EventMessage> ExecuteAppAsync(string uuid, string appName, string appArg);

        Task<ApiResponse> SendApiAsync(string command);

        Task<BackgroundJobResult> BgApi(string command, string arg = null, Guid? jobUUID = null);

        Task<CommandReply> SendCommandAsync(string command);
    }
}