using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.game.PlayerParticipant")]
  public class PlayerParticipant : Participant {
    [SerializedName("timeAddedToQueue")]
    public double TimeAddedToQueue { get; set; }

    [SerializedName("index")]
    public int Index { get; set; }

    [SerializedName("team")]
    public int Team { get; set; }

    [SerializedName("teamName")]
    public int TeamName { get; set; }

    [SerializedName("summonerLevel")]
    public int Summonerlevel { get; set; }

    [SerializedName("queueRating")]
    public int QueueRating { get; set; }

    [SerializedName("accountId")]
    public long AccountId { get; set; }

    [SerializedName("botDifficulty")]
    public string BotDifficulty { get; set; }

    [SerializedName("originalAccountNumber")]
    public double OriginalAccountNumber { get; set; }

    [SerializedName("summonerInternalName")]
    public string SummonerInternalName { get; set; }

    [SerializedName("minor")]
    public bool Minor { get; set; }

    [SerializedName("locale")]
    public object Locale { get; set; }

    [SerializedName("lastSelectedSkinIndex")]
    public int LastSelectedSkinIndex { get; set; }

    [SerializedName("partnerId")]
    public string PartnerId { get; set; }

    [SerializedName("profileIconId")]
    public int ProfileIconId { get; set; }

    [SerializedName("teamOwner")]
    public bool TeamOwner { get; set; }

    [SerializedName("summonerId")]
    public long SummonerId { get; set; }

    [SerializedName("badges")]
    public int Badges { get; set; }

    [SerializedName("pickTurn")]
    public int PickTurn { get; set; }

    [SerializedName("clientInSynch")]
    public bool ClientInSynch { get; set; }

    [SerializedName("summonerName")]
    public string SummonerName { get; set; }

    [SerializedName("pickMode")]
    public int PickMode { get; set; }

    [SerializedName("originalPlatformId")]
    public string OriginalPlatformId { get; set; }

    [SerializedName("teamParticipantId")]
    public double TeamParticipantId { get; set; }

    [SerializedName("pointSummary")]
    public PointSummary PointSummary { get; set; }
  }
}
