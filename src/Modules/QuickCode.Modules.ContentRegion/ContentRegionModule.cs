using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using QuickCode.Core;
using QuickCode.Modules.ContentRegion.Views;

namespace QuickCode.Modules.ContentRegion
{
    public class ContentRegionModule : IModule
    {
        private readonly IRegionManager mRegionManager;

        public ContentRegionModule(IRegionManager regionManager)
        {
            mRegionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            mRegionManager.RequestNavigate(RegionNames.ContentRegion, nameof(ContentRegionView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ContentRegionView>();
        }
    }
}