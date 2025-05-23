using QuickCode.Services.Interfaces.AppSettingsDependency;

namespace QuickCode.Services
{
    public class AppSettings 
    {
        public ClientSettings ClientSettings { get ; set ; } = new ClientSettings();
    }
}
