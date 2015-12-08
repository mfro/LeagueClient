using LeagueClient.Logic.com.riotgames.JSON;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class SummonerCache {
    public class Item {
      public AllPublicSummonerDataDTO AllData { get; private set; }
      public SummonerLeaguesDTO Leagues { get; private set; }
      public PublicSummoner Summoner { get; private set; }
      public List<ChampionMasteryDTO> ChampionMastery { get; private set; }

      public static async Task<Item> Generate(string summonerName) {
        try {
          string mastery = null;
          var pubSumm = await RiotServices.SummonerService.GetSummonerByName(summonerName);
          var guid = RiotServices.ChampionMasteryService.GetAllChampionMasteries(pubSumm.SummonerId);
          RiotServices.AddHandler(guid, res => mastery = res.payload);
          var leagues = await RiotServices.LeaguesService.GetAllLeaguesForPlayer(pubSumm.SummonerId);
          var all = await RiotServices.SummonerService.GetAllPublicSummonerDataByAccount(pubSumm.AcctId);
          var masteryList = (List<ChampionMasteryDTO>) (dynamic) (JSON.ParseArray(mastery));

          return new Item { AllData = all, Summoner = pubSumm, Leagues = leagues, ChampionMastery = masteryList };
        } catch (Exception x) {
          Client.Log("Exception fetching summoner: " + x);
          return null;
        }
      }

      public override int GetHashCode() => Summoner.AcctId.GetHashCode();
      public override bool Equals(object obj) => obj is Item && ((Item) obj).Summoner.AcctId == Summoner.AcctId;
    }

    private Dictionary<long, Item> idCache = new Dictionary<long, Item>();
    private Dictionary<string, Item> nameCache = new Dictionary<string, Item>();

    public SummonerCache() {

    }

    public async void GetData(string summonerName, Action<Item> callback) {
      summonerName = Minimize(summonerName);
      if (nameCache.ContainsKey(summonerName)) callback(nameCache[summonerName]);

      var item = await Item.Generate(summonerName);

      if (item != null) {
        nameCache[summonerName] = item;
        idCache[item.Summoner.SummonerId] = item;
      }

      callback(item);
    }

    private static string Minimize(string summonername) => Regex.Replace(summonername, @"\s+", "").ToLower();
  }
}
