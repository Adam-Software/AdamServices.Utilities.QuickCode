using System;
using Microsoft.Extensions.DependencyInjection;
using Prism.Navigation;
using Prism.Regions;

namespace QuickCode.Core.Mvvm
{
    public class RegionViewModelBase : ViewModelBase, INavigationAware, IConfirmNavigationRequest, IDestructible
    {
        protected IRegionManager RegionManager { get; private set; }

        public RegionViewModelBase(IRegionManager regionManager)
        {
            RegionManager = regionManager;
        }

        public RegionViewModelBase(IServiceProvider serviceProvider)
        {
            RegionManager = serviceProvider.GetService<IRegionManager>();
        }

        /// <summary>
        /// Occurs when the navigation area is called
        /// </summary>
        public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            continuationCallback(true);
        }

        public virtual bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        /// <summary>
        /// On close region
        /// </summary>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        /// <summary>
        /// On load region
        /// </summary>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {

        }
    }
}
