using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.RemovedFromLobbyNotification")]
  public class RemovedFromLobbyNotification {
    [SerializedName("removalReason")]
    public System.String removalReason { get; set; }
    [SerializedName("removalReasonAsString")]
    public System.String removalReasonAsString { get; set; }
  }
}
