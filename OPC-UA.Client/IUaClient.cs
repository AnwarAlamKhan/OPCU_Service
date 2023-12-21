using OPC_UA.Client.Common;

namespace OPC_UA.Client
{
    public interface IUaClient
    {
        public void Connect();
        
        public ReadEvent<T> Read<T>(string tag);
        public Task<ReadEvent<T>> ReadAsync<T>(string tag);

        public void Write<T>(string tag, T item);
        public Task WriteAsync<T>(string tag, T item);

        public void Monitor<T>(string tag, Action<ReadEvent<T>, Action> callback, int monitoringIntervalMs = 100);

        public OpcStatus Status { get; }

        public event EventHandler ServerConnectionLost;

        public event EventHandler ServerConnectionRestored;

        public void Dispose();

        public void ReConnect();

        public void RecreateSession();

    }
}
