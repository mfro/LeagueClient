using System;
using RtmpSharp.IO;
using System.Collections.Generic;
using LeagueClient.Logic.Riot.Team;

namespace LeagueClient.Logic.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.statistics.EndOfGameStats")]
  public class EndOfGameStats {
    [SerializedName("otherTeamInfo")]
    public TeamInfo OtherTeamInfo { get; set; }

    [SerializedName("newSpells")]
    public List<object> NewSpells { get; set; }

    [SerializedName("roomName")]
    public string RoomName { get; set; }

    [SerializedName("otherTeamPlayerParticipantStats")]
    public List<object> OtherTeamPlayerParticipantStats { get; set; }

    [SerializedName("rerollBonusEarned")]
    public double RerollBonusEarned { get; set; }

    [SerializedName("talentPointsGained")]
    public double TalentPointsGained { get; set; }

    [SerializedName("boostXpEarned")]
    public double BoostXpEarned { get; set; }

    [SerializedName("gameType")]
    public string GameType { get; set; }

    [SerializedName("userId")]
    public double UserId { get; set; }

    [SerializedName("rpEarned")]
    public double RpEarned { get; set; }

    [SerializedName("ipTotal")]
    public double IpTotal { get; set; }

    [SerializedName("coOpVsAiMinutesLeftToday")]
    public double CoOpVsAiMinutesLeftToday { get; set; }

    [SerializedName("completionBonusPoints")]
    public double CompletionBonusPoints { get; set; }

    [SerializedName("practiceMinutesPlayedToday")]
    public double PracticeMinutesPlayedToday { get; set; }

    [SerializedName("customMsecsUntilReset")]
    public double CustomMsecsUntilReset { get; set; }

    [SerializedName("firstWinBonus")]
    public double FirstWinBonus { get; set; }

    [SerializedName("myTeamInfo")]
    public TeamInfo MyTeamInfo { get; set; }

    [SerializedName("eloChange")]
    public int EloChange { get; set; }

    [SerializedName("ipEarned")]
    public double IpEarned { get; set; }

    [SerializedName("experienceEarned")]
    public double ExperienceEarned { get; set; }

    [SerializedName("odinBonusIp")]
    public double OdinBonusIp { get; set; }

    [SerializedName("expPointsToNextLevel")]
    public double ExpPointsToNextLevel { get; set; }

    [SerializedName("loyaltyBoostIpEarned")]
    public double LoyaltyBoostIpEarned { get; set; }

    [SerializedName("locationBoostXpEarned")]
    public double LocationBoostXpEarned { get; set; }

    [SerializedName("gameId")]
    public double GameId { get; set; }

    [SerializedName("roomPassword")]
    public string RoomPassword { get; set; }

    [SerializedName("elo")]
    public int Elo { get; set; }

    [SerializedName("gameMode")]
    public string GameMode { get; set; }

    [SerializedName("teamPlayerParticipantStats")]
    public List<object> TeamPlayerParticipantStats { get; set; }

    [SerializedName("customMinutesLeftToday")]
    public double CustomMinutesLeftToday { get; set; }

    [SerializedName("coOpVsAiMsecsUntilReset")]
    public double CoOpVsAiMsecsUntilReset { get; set; }

    [SerializedName("partyRewardsBonusIpEarned")]
    public double PartyRewardsBonusIpEarned { get; set; }

    [SerializedName("reportGameId")]
    public double ReportGameId { get; set; }

    [SerializedName("imbalancedTeamsNoPoints")]
    public bool ImbalancedTeamsNoPoints { get; set; }

    [SerializedName("basePoints")]
    public double BasePoints { get; set; }

    [SerializedName("leveledUp")]
    public bool LeveledUp { get; set; }

    [SerializedName("boostIpEarned")]
    public double BoostIpEarned { get; set; }

    [SerializedName("pointsPenalties")]
    public List<object> PointsPenalties { get; set; }

    [SerializedName("skinIndex")]
    public double SkinIndex { get; set; }

    [SerializedName("battleBoostIpEarned")]
    public double BattleBoostIpEarned { get; set; }

    [SerializedName("sendStatsToTournamentProvider")]
    public bool SendStatsToTournamentProvider { get; set; }

    [SerializedName("gameLength")]
    public double GameLength { get; set; }

    [SerializedName("myTeamStatus")]
    public string MyTeamStatus { get; set; }

    [SerializedName("locationBoostIpEarned")]
    public double LocationBoostIpEarned { get; set; }

    [SerializedName("gameMutators")]
    public List<object> GameMutators { get; set; }

    [SerializedName("queueBonusEarned")]
    public double QueueBonusEarned { get; set; }

    [SerializedName("skinName")]
    public string SkinName { get; set; }

    [SerializedName("loyaltyBoostXpEarned")]
    public double LoyaltyBoostXpEarned { get; set; }

    [SerializedName("timeUntilNextFirstWinBonus")]
    public double TimeUntilNextFirstWinBonus { get; set; }

    [SerializedName("difficulty")]
    public string Difficulty { get; set; }

    [SerializedName("ranked")]
    public bool Ranked { get; set; }

    [SerializedName("rerollEarned")]
    public double RerollEarned { get; set; }

    [SerializedName("queueType")]
    public string QueueType { get; set; }

    [SerializedName("rerollEnabled")]
    public bool RerollEnabled { get; set; }

    [SerializedName("invalid")]
    public bool Invalid { get; set; }

    [SerializedName("experienceTotal")]
    public double ExperienceTotal { get; set; }
  }
}
