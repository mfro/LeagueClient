using System;
using RtmpSharp.IO;

namespace LeagueClient.Logic.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.summoner.PublicSummoner")]
  public class PublicSummoner {
    [SerializedName("internalName")]
    public String InternalName { get; set; }

    [SerializedName("acctId")]
    public Int64 AcctId { get; set; }

    [SerializedName("name")]
    public String Name { get; set; }

    [SerializedName("profileIconId")]
    public Int32 ProfileIconId { get; set; }

    [SerializedName("revisionDate")]
    public DateTime RevisionDate { get; set; }

    [SerializedName("revisionId")]
    public Double RevisionId { get; set; }

    [SerializedName("summonerLevel")]
    public Int32 SummonerLevel { get; set; }

    [SerializedName("summonerId")]
    public Int64 SummonerId { get; set; }
  }
}
