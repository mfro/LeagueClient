using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.statistics.PlayerStatSummaries")]
  public class PlayerStatSummaries {
    [SerializedName("playerStatSummarySet")]
    public List<PlayerStatSummary> PlayerStatSummarySet { get; set; }

    [SerializedName("userId")]
    public Double UserId { get; set; }

    [SerializedName("season")]
    public Int32 Season { get; set; }
  }
}
