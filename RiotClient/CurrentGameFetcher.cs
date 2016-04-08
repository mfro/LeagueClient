using MFroehlich.League.RiotAPI;
using RiotClient;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CurrentGame = MFroehlich.League.RiotAPI.RiotAPI.CurrentGameAPI.CurrentGameInfo;

namespace RiotClient {
  public static class CurrentGameFetcher {
    private static List<long> inProgress = new List<long>();

    public static async void FetchGame(long summonerId, Action<CurrentGame> callback) {
      if (inProgress.Contains(summonerId)) return;
      inProgress.Add(summonerId);

      CurrentGame game;
      while (true) {
        try {
          game = await RiotAPI.CurrentGameAPI.BySummonerAsync(Session.Region.Platform, summonerId);
        } catch {
          await Task.Delay(20000);
          continue;
        }


        callback(game);
        if (game.gameStartTime == 0) await Task.Delay(20000);
        else break;
      }

      inProgress.Remove(summonerId);
    }
  }
}
