using MFroehlich.League.RiotAPI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CurrentGame = MFroehlich.League.RiotAPI.RiotAPI.CurrentGameAPI.CurrentGameInfo;

namespace LeagueClient.Logic {

  public static class CurrentGameFetcher {
    private static Dictionary<long, Action<CurrentGame>> callbacks = new Dictionary<long, Action<CurrentGame>>();

    public static void FetchGame(long summonerId, Action<CurrentGame> callback) {
      callbacks[summonerId] = callback;
      FetchGame(summonerId);
    }

    private static async void FetchGame(long summonerId) {
      CurrentGame game;

      while (true) {
        game = await RiotAPI.CurrentGameAPI.BySummonerAsync(Client.Region.Platform, summonerId);
        DistributeGame(game);

        if (game.gameStartTime == 0) await Task.Delay(10000);
        else break;
      }
    }

    private static void DistributeGame(CurrentGame game) {
      foreach (var player in game.participants) {
        Action<CurrentGame> callback;
        if (callbacks.TryGetValue(player.summonerId, out callback)) {
          callback(game);

          if (game.gameStartTime == 0) callbacks.Remove(player.summonerId);
        }
      }
    }
  }
}
