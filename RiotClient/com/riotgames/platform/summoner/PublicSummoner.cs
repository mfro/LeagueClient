using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.summoner.PublicSummoner")]
  public class PublicSummoner : BaseSummoner {
    [SerializedName("summonerLevel")]
    public Int32 SummonerLevel { get; set; }
  }
}
