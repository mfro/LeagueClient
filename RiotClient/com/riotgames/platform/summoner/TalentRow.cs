using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.summoner.TalentRow")]
  public class TalentRow {
    [SerializedName("index")]
    public Int32 Index { get; set; }

    [SerializedName("talents")]
    public List<Talent> Talents { get; set; }

    [SerializedName("tltGroupId")]
    public Int32 TltGroupId { get; set; }

    [SerializedName("pointsToActivate")]
    public Int32 PointsToActivate { get; set; }

    [SerializedName("tltRowId")]
    public Int32 TltRowId { get; set; }

    [SerializedName("maxPointsInRow")]
    public Int32 MaxPointsInRow { get; set; }
  }
}
