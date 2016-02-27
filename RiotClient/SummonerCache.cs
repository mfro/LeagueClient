using MFroehlich.Parsing.JSON;
using RiotClient.Riot.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RiotClient {
  public class SummonerCache {
    public class Item {
      public AllPublicSummonerDataDTO Data { get; private set; }
      public SummonerLeaguesDTO Leagues { get; private set; }
      public RiotACS.PlayerHistory MatchHistory { get; private set; }

      //public List<ChampionMasteryDTO> ChampionMastery { get; private set; }

      public static async Task<Item> Generate(string summonerName, Action<Item> callback) {
        try {
          var summ = await RiotServices.SummonerService.GetSummonerByName(summonerName);
          return await Generate(summ.AccountId, callback);
        } catch (Exception x) {
          Session.Log("Exception fetching summoner: " + x);
          return null;
        }
      }

      public static async Task<Item> Generate(long accountId, Action<Item> callback) {
        try {
          var item = new Item();
          item.Data = await RiotServices.SummonerService.GetAllPublicSummonerDataByAccount(accountId);
          callback(item);

          //var guid = RiotServices.ChampionMasteryService.GetAllChampionMasteries(item.Data.Summoner.SummonerId);
          //RiotServices.AddHandler(guid, res => item.ChampionMastery = JSONDeserializer.Deserialize<List<ChampionMasteryDTO>>(JSONParser.ParseArray(res.payload, 0)));

          item.Leagues = await RiotServices.LeaguesService.GetAllLeaguesForPlayer(item.Data.Summoner.SummonerId);
          callback(item);

          item.MatchHistory = await RiotACS.GetMatchHistory(Session.Region.Platform, item.Data.Summoner.AccountId);
          callback(item);

          return item;
        } catch (Exception x) {
          Session.Log("Exception fetching summoner: " + x);
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

      await Item.Generate(summonerName, item => {
        if (!nameCache.ContainsKey(summonerName))
          nameCache[summonerName] = item;
        if (!idCache.ContainsKey(item.Data.Summoner.SummonerId))
          idCache[item.Data.Summoner.SummonerId] = item;
        if (!accountCache.ContainsKey(item.Data.Summoner.AccountId))
          accountCache[item.Data.Summoner.AccountId] = item;

        callback(item);
      });
    }

    public async void GetData(long accountId, Action<Item> callback) {
      if (accountCache.ContainsKey(accountId)) callback(accountCache[accountId]);

      var item = await Item.Generate(accountId, callback);

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
