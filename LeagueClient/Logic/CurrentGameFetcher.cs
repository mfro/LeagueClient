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
    public static async void FetchGame(long summonerId, Action<CurrentGame> callback) {
      CurrentGame game;

      while (true) {
        game = await RiotAPI.CurrentGameAPI.BySummonerAsync(Client.Region.Platform, summonerId);

        callback(game);
        if (game.gameStartTime == 0) await Task.Delay(20000);
        else break;
      }
    }
  }
}
