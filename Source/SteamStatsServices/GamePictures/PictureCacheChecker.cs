﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Trfc.ClientFramework;

namespace Trfc.SteamStats.ClientServices.GamePictures
{
    public sealed class PictureCacheChecker : IPictureCacheChecker
    {
        private readonly string connectionStringKey = "GamePictureCacheTime";

        private readonly IConfigurationProvider configurationProvider;
        private readonly IWebGateway webGateway;

        public PictureCacheChecker(IConfigurationProvider configurationProvider,
            IWebGateway webGateway)
        {
            this.configurationProvider = configurationProvider;
            this.webGateway = webGateway;
        }

        public async Task<CacheResponse> IsCacheOutOfDate(DateTime cacheLastUpdatedTimeUtc, int appId, CancellationToken token)
        {
            var endpoint = configurationProvider.GetConnectionStringById(connectionStringKey);

            endpoint += $"?appId={appId}";

            var gatewayResponse = await webGateway.GetResponseFromEndpoint<ResponseDao>(endpoint, token);

            if (gatewayResponse.ResultCode != WebRequestResponseResultCode.Succeeded)
            {
                return CacheResponse.CacheCheckFailed(gatewayResponse.ResultMessage);
            }

            var response = gatewayResponse.Value;

            if (!response.IsCached)
            {
                return CacheResponse.OutOfDate();
            }

            if (response.CachedDateTimeUtc < cacheLastUpdatedTimeUtc)
            {
                return CacheResponse.NotOutOfDate();
            }

            return CacheResponse.OutOfDate();
        }

        [Preserve(AllMembers = true)]
        private sealed class ResponseDao
        {
            public bool IsCached { get; set; }
            public DateTime CachedDateTimeUtc { get; set; }
            public ResponseDao(bool isCached, DateTime cachedDateTimeUtc)
            {
                IsCached = isCached;
                CachedDateTimeUtc = cachedDateTimeUtc;
            }
        }
    }
}
