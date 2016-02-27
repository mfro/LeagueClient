using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.Inviter")]
  public class Inviter {
    [SerializedName("previousSeasonHighestTier")]
    public System.String previousSeasonHighestTier { get; set; }
    [SerializedName("summonerName")]
    public System.String summonerName { get; set; }
    [SerializedName("summonerId")]
    public System.Int64 summonerId { get; set; }
  }
}
