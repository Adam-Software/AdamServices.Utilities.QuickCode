namespace QuickCode.Services.Interfaces.AppSettingsDependency
{
    public class ClientSettings
    {
        private string mIp = "127.0.0.1";
        public string Ip
        {
            get { return mIp; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    mIp = "127.0.0.1";
                    return;
                }

                mIp = value;
            }
        }
        public int Port { get; set; } = 19000;
    }
}
