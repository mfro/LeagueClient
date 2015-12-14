using System;
using RtmpSharp.IO;

namespace LeagueClient.Logic.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.game.AramPlayerParticipant")]
  public class AramPlayerParticipant : PlayerParticipant {
    [SerializedName("pointSummary")]
    public PointSummary PointSummary { get; set; }
  }
}