using MFroehlich.League.RiotAPI;
using MFroehlich.Parsing.JSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient {
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
      var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Session.Current.LoginQueue.GasToken.ToJSON()));
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
        return JSONDeserializer.Deserialize<T>(JSONParser.ParseArray(data, 0));
      return JSONDeserializer.Deserialize<T>(JSONParser.ParseObject(data, 0));
    }

    #region Deltas

    public class PlayerDeltas {
      [JSONField("originalAccountId")]
      public long OriginalAccountId { get; set; }

      [JSONField("originalPlatformId")]
      public string OriginalPlatform { get; set; }

      [JSONField("deltas")]
      public List<GameDeltaInfo> Deltas { get; set; }
    }

    public class GameDeltaInfo {
      [JSONField("gamePlatformId")]
      public string Platform { get; set; }

      [JSONField("gameId")]
      public long GameId { get; set; }

      [JSONField("platformDelta")]
      public Delta Delta { get; set; }
    }

    public class Delta {
      [JSONField("gamePlatformId")]
      public string Platform { get; set; }

      [JSONField("gameId")]
      public long GameId { get; set; }

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
    public class PlayerHistory : JSONSerializable {
      [JSONField("platformId")]
      public string Platform { get; set; }

      [JSONField("accountId")]
      public long AccountId { get; set; }

      [JSONField("shownQueues")]
      public List<int> ShownQueues { get; set; }

      [JSONField("games")]
      public GameResponseInfo Games { get; set; }
    }

    public class GameResponseInfo : JSONSerializable {
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

    public class Game : JSONSerializable {
      [JSONField("gameId")]
      public long GameId { get; set; }

      [JSONField("platformId")]
      public string Platform { get; set; }

      [JSONField("gameCreation")]
      public long GameCreation { get; set; }

      [JSONField("gameDuration")]
      public int GameDuration { get; set; }

      [JSONField("queueId")]
      public int QueueId { get; set; }

      [JSONField("mapId")]
      public int MapId { get; set; }

      [JSONField("seasonId")]
      public int SeasonId { get; set; }

      [JSONField("gameVersion")]
      public string GameVersion { get; set; }

      [JSONField("gameMode")]
      public string GameMode { get; set; }

      [JSONField("gameType")]
      public string GameType { get; set; }

      [JSONField("teams")]
      public List<Team> Teams { get; set; }

      [JSONField("participants")]
      public List<Participant> Participants { get; set; }

      [JSONField("participantIdentities")]
      public List<ParticipantIdentity> ParticipantIdentities { get; set; }
    }

    public class Participant : JSONSerializable {
      [JSONField("participantId")]
      public int ParticipantId { get; set; }

      [JSONField("teamId")]
      public int TeamId { get; set; }

      [JSONField("championId")]
      public int ChampionId { get; set; }

      [JSONField("spell1Id")]
      public int Spell1Id { get; set; }

      [JSONField("spell2Id")]
      public int Spell2Id { get; set; }

      [JSONField("masteries")]
      public List<RiotAPI.MatchAPI.Mastery> Masteries { get; set; }

      [JSONField("runes")]
      public List<RiotAPI.MatchAPI.Rune> Runes { get; set; }

      [JSONField("stats")]
      public ParticipantStats Stats { get; set; }

      [JSONField("timeline")]
      public RiotAPI.MatchAPI.ParticipantTimeline Timeline { get; set; }

      [JSONField("highestAchievedSeasonTier")]
      public string HighestAchievedSeasonTier { get; set; }
    }

    public class ParticipantStats : JSONSerializable {
      [JSONField("participantId")]
      public Int32 ParticipantId { get; set; }

      [JSONField("win")]
      public Boolean Win { get; set; }

      [JSONField("item0")]
      public Int32 Item0 { get; set; }

      [JSONField("item1")]
      public Int32 Item1 { get; set; }

      [JSONField("item2")]
      public Int32 Item2 { get; set; }

      [JSONField("item3")]
      public Int32 Item3 { get; set; }

      [JSONField("item4")]
      public Int32 Item4 { get; set; }

      [JSONField("item5")]
      public Int32 Item5 { get; set; }

      [JSONField("item6")]
      public Int32 Item6 { get; set; }

      [JSONField("kills")]
      public Int32 Kills { get; set; }

      [JSONField("deaths")]
      public Int32 Deaths { get; set; }

      [JSONField("assists")]
      public Int32 Assists { get; set; }

      [JSONField("largestKillingSpree")]
      public Int32 LargestKillingSpree { get; set; }

      [JSONField("largestMultiKill")]
      public Int32 LargestMultiKill { get; set; }

      [JSONField("killingSprees")]
      public Int32 KillingSprees { get; set; }

      [JSONField("longestTimeSpentLiving")]
      public Int32 LongestTimeSpentLiving { get; set; }

      [JSONField("doubleKills")]
      public Int32 DoubleKills { get; set; }

      [JSONField("tripleKills")]
      public Int32 TripleKills { get; set; }

      [JSONField("quadraKills")]
      public Int32 QuadraKills { get; set; }

      [JSONField("pentaKills")]
      public Int32 PentaKills { get; set; }

      [JSONField("unrealKills")]
      public Int32 UnrealKills { get; set; }

      [JSONField("totalDamageDealt")]
      public Int32 TotalDamageDealt { get; set; }

      [JSONField("magicDamageDealt")]
      public Int32 MagicDamageDealt { get; set; }

      [JSONField("physicalDamageDealt")]
      public Int32 PhysicalDamageDealt { get; set; }

      [JSONField("trueDamageDealt")]
      public Int32 TrueDamageDealt { get; set; }

      [JSONField("largestCriticalStrike")]
      public Int32 LargestCriticalStrike { get; set; }

      [JSONField("totalDamageDealtToChampions")]
      public Int32 TotalDamageDealtToChampions { get; set; }

      [JSONField("magicDamageDealtToChampions")]
      public Int32 MagicDamageDealtToChampions { get; set; }

      [JSONField("physicalDamageDealtToChampions")]
      public Int32 PhysicalDamageDealtToChampions { get; set; }

      [JSONField("trueDamageDealtToChampions")]
      public Int32 TrueDamageDealtToChampions { get; set; }

      [JSONField("totalHeal")]
      public Int32 TotalHeal { get; set; }

      [JSONField("totalUnitsHealed")]
      public Int32 TotalUnitsHealed { get; set; }

      [JSONField("totalDamageTaken")]
      public Int32 TotalDamageTaken { get; set; }

      [JSONField("magicalDamageTaken")]
      public Int32 MagicalDamageTaken { get; set; }

      [JSONField("physicalDamageTaken")]
      public Int32 PhysicalDamageTaken { get; set; }

      [JSONField("trueDamageTaken")]
      public Int32 TrueDamageTaken { get; set; }

      [JSONField("goldEarned")]
      public Int32 GoldEarned { get; set; }

      [JSONField("goldSpent")]
      public Int32 GoldSpent { get; set; }

      [JSONField("turretKills")]
      public Int32 TurretKills { get; set; }

      [JSONField("inhibitorKills")]
      public Int32 InhibitorKills { get; set; }

      [JSONField("totalMinionsKilled")]
      public Int32 TotalMinionsKilled { get; set; }

      [JSONField("neutralMinionsKilled")]
      public Int32 NeutralMinionsKilled { get; set; }

      [JSONField("neutralMinionsKilledTeamJungle")]
      public Int32 NeutralMinionsKilledTeamJungle { get; set; }

      [JSONField("neutralMinionsKilledEnemyJungle")]
      public Int32 NeutralMinionsKilledEnemyJungle { get; set; }

      [JSONField("totalTimeCrowdControlDealt")]
      public Int32 TotalTimeCrowdControlDealt { get; set; }

      [JSONField("champLevel")]
      public Int32 ChampLevel { get; set; }

      [JSONField("visionWardsBoughtInGame")]
      public Int32 VisionWardsBoughtInGame { get; set; }

      [JSONField("sightWardsBoughtInGame")]
      public Int32 SightWardsBoughtInGame { get; set; }

      [JSONField("wardsPlaced")]
      public Int32 WardsPlaced { get; set; }

      [JSONField("wardsKilled")]
      public Int32 WardsKilled { get; set; }

      [JSONField("firstBloodKill")]
      public Boolean FirstBloodKill { get; set; }

      [JSONField("firstBloodAssist")]
      public Boolean FirstBloodAssist { get; set; }

      [JSONField("firstTowerKill")]
      public Boolean FirstTowerKill { get; set; }

      [JSONField("firstTowerAssist")]
      public Boolean FirstTowerAssist { get; set; }

      [JSONField("firstInhibitorKill")]
      public Boolean FirstInhibitorKill { get; set; }

      [JSONField("firstInhibitorAssist")]
      public Boolean FirstInhibitorAssist { get; set; }

      [JSONField("combatPlayerScore")]
      public Int32 CombatPlayerScore { get; set; }

      [JSONField("objectivePlayerScore")]
      public Int32 ObjectivePlayerScore { get; set; }

      [JSONField("totalPlayerScore")]
      public Int32 TotalPlayerScore { get; set; }

      [JSONField("totalScoreRank")]
      public Int32 TotalScoreRank { get; set; }
    }

    public class Team : JSONSerializable {
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

    public class ParticipantIdentity : JSONSerializable {
      [JSONField("participantId")]
      public int ParticipantId { get; set; }

      [JSONField("player")]
      public Player Player { get; set; }
    }

    public class Player : JSONSerializable {
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
