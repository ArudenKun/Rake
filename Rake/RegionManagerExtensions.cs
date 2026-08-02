using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNavigation.Abstractions;
using AsyncNavigation.Core;
using JetBrains.Annotations;
using Volo.Abp.Threading;

namespace Rake;

[PublicAPI]
public static class RegionManagerExtensions
{
    extension(IRegionManager regionManager)
    {
        public NavigationResult RequestNavigate<TView>(
            string regionName,
            INavigationParameters? navigationParameters = null,
            bool replay = false,
            CancellationToken cancellationToken = default
        )
            where TView : IView
        {
            var result = AsyncHelper.RunSync(() =>
                regionManager.RequestNavigateAsync<TView>(
                    regionName,
                    navigationParameters,
                    replay,
                    cancellationToken
                )
            );
            return result;
        }

        public async Task<NavigationResult> RequestNavigateAsync<TView>(
            string regionName,
            INavigationParameters? navigationParameters = null,
            bool replay = false,
            CancellationToken cancellationToken = default
        )
            where TView : IView =>
            await regionManager.RequestNavigateAsync(
                regionName,
                typeof(TView).GetFullNameWithAssemblyName(),
                navigationParameters,
                replay,
                cancellationToken
            );

        public NavigationResult RequestNavigate(
            Type viewType,
            string regionName,
            INavigationParameters? navigationParameters = null,
            bool replay = false,
            CancellationToken cancellationToken = default
        )
        {
            var result = AsyncHelper.RunSync(() =>
                regionManager.RequestNavigateAsync(
                    viewType,
                    regionName,
                    navigationParameters,
                    replay,
                    cancellationToken
                )
            );
            return result;
        }

        public async Task<NavigationResult> RequestNavigateAsync(
            Type viewType,
            string regionName,
            INavigationParameters? navigationParameters = null,
            bool replay = false,
            CancellationToken cancellationToken = default
        ) =>
            await regionManager.RequestNavigateAsync(
                regionName,
                viewType.GetFullNameWithAssemblyName(),
                navigationParameters,
                replay,
                cancellationToken
            );

        public IRegion GetRegion(string regionName) =>
            regionManager.TryGetRegion(regionName, out var region)
                ? region
                : throw new InvalidOperationException($"Could not find region '{regionName}'");
    }
}
