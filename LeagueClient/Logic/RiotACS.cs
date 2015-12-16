using MFroehlich.League.RiotAPI;
using MFroehlich.Parsing.JSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public static class RiotACS {
    public static async Task<PlayerHistory> GetMatchHistory(string platform, long accountId) {
      return await FetchAsync<PlayerHistory>($"https://acs.leagueoflegends.com/v1/stats/player_history/{platform}/{accountId}?begIndex=0&endIndex=20");
    }

    public static async Task<RiotAPI.MatchAPI.Timeline> GetMatchTimeline(string platform, long gameId) {
      return await FetchAsync<RiotAPI.MatchAPI.Timeline>($"https://acs.leagueoflegends.com/v1/stats/game/{platform}/{gameId}/timeline");
    }

    public static async Task<Game> GetMatchDetails(string platform, long gameId) {
      return await FetchAsync<Game>($"https://acs.leagueoflegends.com/v1/stats/game/{platform}/{gameId}");
    }

    public static async Task<PlayerDeltas> GetDeltas() {
      return await FetchAsync<PlayerDeltas>($"https://acs.leagueoflegends.com/v1/deltas/auth");
    }

    private static async Task<T> FetchAsync<T>(string url) where T : new() {
      byte[] data = null;
      var req = System.Net.WebRequest.Create(url);
      var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Client.LoginQueue.GasToken.ToJSON()));
      req.Headers.Add("REGION", "NA1");
      req.Headers.Add("AUTHORIZATION", "GasTokenRaw " + b64);

      System.Net.WebResponse res;
      try { res = await req.GetResponseAsync(); } catch (System.Net.WebException x) { res = x.Response; }
      using (res)
      using (var mem = new System.IO.MemoryStream()) {
        res.GetResponseStream().CopyTo(mem);
        data = mem.ToArray();
      }
      if (typeof(System.Collections.IList).IsAssignableFrom(typeof(T)))
        return JSONParser.ParseArray(data, 0).Fill<T>();
      return JSONParser.ParseObject(data, 0).Fill(new T());
    }

    #region Deltas

    [JSONSerializable]
    public class PlayerDeltas {
      [JSONField("originalAccountId")]
      public long OriginalAccountId { get; set; }

      [JSONField("originalPlatformId")]
      public string OriginalPlatform { get; set; }

      [JSONField("deltas")]
      public List<GameDeltaInfo> Deltas { get; set; }
    }

    [JSONSerializable]
    public class GameDeltaInfo {
      [JSONField("gamePlatformId")]
      public string Platform { get; set; }

      [JSONField("gameId")]
      public long GameId { get; set; }

      [JSONField("platformDelta")]
      public Delta Delta { get; set; }
    }

    [JSONSerializable]
    public class Delta {
      [JSONField("xpDelta")]
      public int XP { get; set; }

      [JSONField("ipDelta")]
      public int IP { get; set; }

      [JSONField("compensationModeEnabled")]
      public bool IsCompensationModeEnabled { get; set; }

      [JSONField("timestamp")]
      public long Timestamp { get; set; }
    }

    #endregion

    #region Games
    [JSONSerializable]
    public class PlayerHistory {
      [JSONField("platformId")]
      public string Platform { get; set; }

      [JSONField("accountId")]
      public long AccountId { get; set; }

      [JSONField("shownQueues")]
      public List<int> ShownQueues { get; set; }

      [JSONField("games")]
      public GameResponseInfo Games { get; set; }
    }

    [JSONSerializable]
    public class GameResponseInfo {
      [JSONField("gameIndexBegin")]
      public int GameIndexBegin { get; set; }

      [JSONField("gameIndexEnd")]
      public int GameIndexEnd { get; set; }

      [JSONField("gameTimestampBegin")]
      public int GameTimestampBegin { get; set; }

      [JSONField("gameTimestampEnd")]
      public int GameTimestampEnd { get; set; }

      [JSONField("gameCount")]
      public int GameCount { get; set; }

      [JSONField("games")]
      public List<Game> Games { get; set; }
    }

    [JSONSerializable]
    public class Game {
      [JSONField("gameId")]
      public int GameId { get; set; }

      [JSONField("platformId")]
      public int Platform { get; set; }

      [JSONField("gameCreation")]
      public int GameCreation { get; set; }

      [JSONField("gameDuration")]
      public int GameDuration { get; set; }

      [JSONField("queueId")]
      public int QueueId { get; set; }

      [JSONField("mapId")]
      public int MapId { get; set; }

      [JSONField("seasonId")]
      public int SeasonId { get; set; }

      [JSONField("gameVersion")]
      public int GameVersion { get; set; }

      [JSONField("gameMode")]
      public int GameMode { get; set; }

      [JSONField("gameType")]
      public int GameType { get; set; }

      [JSONField("teams")]
      public List<Team> Teams { get; set; }

      [JSONField("participants")]
      public List<RiotAPI.MatchAPI.Participant> Participants { get; set; }

      [JSONField("participantIdentities")]
      public List<ParticipantIdentity> ParticipantIdentities { get; set; }
    }

    [JSONSerializable]
    public class Team {
      [JSONField("teamId")]
      public int TeamId { get; set; }

      [JSONField("win")]
      public string Win { get; set; }

      [JSONField("firstBlood")]
      public bool FirstBlood { get; set; }

      [JSONField("firstTower")]
      public bool FirstTower { get; set; }

      [JSONField("firstInhibitor")]
      public bool FirstInhibitor { get; set; }

      [JSONField("firstBaron")]
      public bool FirstBaron { get; set; }

      [JSONField("firstDragon")]
      public bool FirstDragon { get; set; }

      [JSONField("towerKills")]
      public int TowerKills { get; set; }

      [JSONField("inhibitorKills")]
      public int InhibitorKills { get; set; }

      [JSONField("baronKills")]
      public int BaronKills { get; set; }

      [JSONField("dragonKills")]
      public int DragonKills { get; set; }

      [JSONField("vilemawKills")]
      public int VilemawKills { get; set; }

      [JSONField("dominionVictoryScore")]
      public int DominionVictoryScore { get; set; }

      [JSONField("bans")]
      public List<RiotAPI.MatchAPI.BannedChampion> Bans { get; set; }
    }

    [JSONSerializable]
    public class ParticipantIdentity {
      [JSONField("participantId")]
      public int ParticipantId { get; set; }

      [JSONField("player")]
      public Player Player { get; set; }
    }

    [JSONSerializable]
    public class Player {
      [JSONField("platformId")]
      public int ParticipantId { get; set; }

      [JSONField("accountId")]
      public long AccountId { get; set; }

      [JSONField("summonerName")]
      public string SummonerName { get; set; }

      [JSONField("summonerId")]
      public long SummonerId { get; set; }

      [JSONField("currentPlatformId")]
      public string CurrentPlatform { get; set; }

      [JSONField("currentAccountId")]
      public long CurrentAccountId { get; set; }

      [JSONField("matchHistoryUri")]
      public string MatchHistoryURI { get; set; }

      [JSONField("profileIcon")]
      public int ProfileIconId { get; set; }
    }
    #endregion
  }
}
