using System;

namespace Collect.Models
{
    [Serializable]
    public class Parameters
    {
        public bool AutoConnect { get; set; } = false;
        public bool AutoReconnect { get; set; } = true;
        public int AutoReconnectTime { get; set; } = 5;
        public DataStorage DataStorage { get; set; } = DataStorage.Cloud;
        public ServerType ServerType { get; set; } = ServerType.Demo;
        public string Login { get; set; } = "";
        public byte[] Password { get; set; }
    }
}
