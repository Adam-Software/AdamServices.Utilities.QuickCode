using QuickCode.Services.Interfaces;
using QuickCode.Services.Interfaces.IAppSettingsServiceDependency;

namespace QuickCode.Services
{
    public class AppSettingService : IAppSettingService
    {
        public ClientSettings ClientSettings { get ; set ; } = new ClientSettings();
    }
}
