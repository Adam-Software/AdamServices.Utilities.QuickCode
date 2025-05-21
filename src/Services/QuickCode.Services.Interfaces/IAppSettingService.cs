using QuickCode.Services.Interfaces.IAppSettingsServiceDependency;


namespace QuickCode.Services.Interfaces
{
    public interface IAppSettingService
    {
        public ClientSettings ClientSettings { get; set; }
    }
}
