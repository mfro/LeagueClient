using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MFroehlich.League.RiotAPI;

namespace LeagueClient.Media {
  public class CurrentGame {
    public static async Task<RiotAPI.CurrentGameAPI.CurrentGameInfo> GetGame(long summoner) {
      var data = await RiotAPI.CurrentGameAPI.BySummonerAsync("NA1", summoner);
      return data;
    }
  }
}
