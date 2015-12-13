using LeagueClient.Logic.com.riotgames.JSON;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public class SummonerCache {
    public class Item {
      public AllPublicSummonerDataDTO Data { get; private set; }
      public SummonerLeaguesDTO Leagues { get; private set; }
      public List<ChampionMasteryDTO> ChampionMastery { get; private set; }

      public static async Task<Item> Generate(string summonerName) {
        try {
          var summ = await RiotServices.SummonerService.GetSummonerByName(summonerName);
          return await Generate(summ.AccountId);
        } catch (Exception x) {
          Client.Log("Exception fetching summoner: " + x);
          return null;
        }
      }

      public static async Task<Item> Generate(long accountId) {
        try {
          var all = await RiotServices.SummonerService.GetAllPublicSummonerDataByAccount(accountId);

          var guid = RiotServices.ChampionMasteryService.GetAllChampionMasteries(all.Summoner.SummonerId);
          var handle = new AutoResetEvent(false);
          string mastery = null;
          RiotServices.AddHandler(guid, res => {
            mastery = res.payload;
            handle.Set();
          });

          var leagues = await RiotServices.LeaguesService.GetAllLeaguesForPlayer(all.Summoner.SummonerId);
          handle.WaitOne(2000);

          if (mastery != null) {
            var masteryList = JSONParser.ParseArray(mastery, 0).Fill<List<ChampionMasteryDTO>>();

            return new Item { Data = all, Leagues = leagues, ChampionMastery = masteryList };
          } else return new Item { Data = all, Leagues = leagues };
        } catch (Exception x) {
          Client.Log("Exception fetching summoner: " + x);
          return null;
        }
      }

      public override int GetHashCode() => Data.Summoner.AccountId.GetHashCode();
      public override bool Equals(object obj) => obj is Item && ((Item) obj).Data.Summoner.AccountId == Data.Summoner.AccountId;
    }

    private Dictionary<long, Item> idCache = new Dictionary<long, Item>();
    private Dictionary<long, Item> accountCache = new Dictionary<long, Item>();
    private Dictionary<string, Item> nameCache = new Dictionary<string, Item>();

    public SummonerCache() {

    }

    public async void GetData(string summonerName, Action<Item> callback) {
      summonerName = Minimize(summonerName);
      if (nameCache.ContainsKey(summonerName)) callback(nameCache[summonerName]);

      var item = await Item.Generate(summonerName);

      if (item != null) {
        nameCache[summonerName] = item;
        idCache[item.Data.Summoner.SummonerId] = item;
        accountCache[item.Data.Summoner.AccountId] = item;
      }

      callback(item);
    }

    public async void GetData(long accountId, Action<Item> callback) {
      if (accountCache.ContainsKey(accountId)) callback(accountCache[accountId]);

      var item = await Item.Generate(accountId);

      if (item != null) {
        nameCache[Minimize(item.Data.Summoner.Name)] = item;
        idCache[item.Data.Summoner.SummonerId] = item;
        accountCache[item.Data.Summoner.AccountId] = item;
      }

      callback(item);
    }

    private static string Minimize(string summonername) => Regex.Replace(summonername, @"\s+", "").ToLower();
  }
}
