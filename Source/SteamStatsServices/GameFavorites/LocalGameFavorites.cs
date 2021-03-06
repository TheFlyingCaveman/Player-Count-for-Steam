﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trfc.ClientFramework;

namespace Trfc.SteamStats.ClientServices.GameFavorites
{
    public sealed class LocalGameFavorites : IGameFavoriter, IFavoriteGameFetcher, IFavoriteGameQuerier
    {
        private readonly IStorageProvider<GameFavoritesDao> storageProvider;
        private readonly IToastMessageService toastMessageService;

        private readonly string storageKey = "FavoriteGames";

        private readonly GameFavorites favoriteIds = new GameFavorites();

        public event EventHandler FavoritesChanged;

        public LocalGameFavorites(IStorageProvider<GameFavoritesDao> storageProvider,
            IToastMessageService toastMessageService)
        {
            this.storageProvider = storageProvider;
            this.toastMessageService = toastMessageService;
        }

        public async Task<bool> FavoriteGameById(int id)
        {
            await RefreshFavorites();

            //TODO: Split out into rules component.
            //TODO: Move max into a configuration value.
            //Decreasing max would break user expectations.
            var maxAmountOfFavorites = 10;
            if(favoriteIds.Count == maxAmountOfFavorites)
            {
                toastMessageService.ShortAlert($"You cannot have more than {maxAmountOfFavorites} favorites!");
                return false;
            }

            favoriteIds.Add(id, true);

            await storageProvider.Update(storageKey, GetCurrentFavoritesAsDao());

            NotifyFavoritesChanged();

            return true;
        }

        public async Task<bool> UnfavoriteGameById(int id)
        {
            await RefreshFavorites();

            favoriteIds.Remove(id);

            await storageProvider.Update(storageKey, GetCurrentFavoritesAsDao());

            NotifyFavoritesChanged();

            return true;
        }

        public async Task<IEnumerable<int>> GetFavoritedGames()
        {
            await RefreshFavorites();

            return this.favoriteIds.Keys.ToList();
        }        
        
        public void NotifyFavoritesChanged()
        {
            FavoritesChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task RefreshFavorites()
        {
            favoriteIds.Clear();

            var response = await storageProvider.Get(storageKey);

            if (response.Succeeded)
            {
                foreach(var id in ((GameFavoritesDao)response.Value).FavoriteIds.ToList())
                {
                    favoriteIds.Add(id, true);
                }
            }
        }

        private GameFavoritesDao GetCurrentFavoritesAsDao()
        {
            return new GameFavoritesDao()
            {
                FavoriteIds = favoriteIds.Keys.ToList()
            };
        }

        public Task<bool> IsGameFavorite(int gameId)
        {
            return Task.FromResult(favoriteIds.ContainsKey(gameId));
        }

        public sealed class GameFavoritesDao
        {
            public IEnumerable<int> FavoriteIds { get; set; }
        }
    }
}
