using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.messaging.persistence.SimpleDialogMessage")]
  public class SimpleDialogMessage {
    [SerializedName("titleCode")]
    public System.String titleCode { get; set; }
    [SerializedName("accountId")]
    public System.Double accountId { get; set; }
    [SerializedName("msgId")]
    public System.String msgId { get; set; }
    [SerializedName("params")]
    public RtmpSharp.IO.AMF3.ArrayCollection @params { get; set; }
    [SerializedName("type")]
    public System.String type { get; set; }
    [SerializedName("bodyCode")]
    public System.String bodyCode { get; set; }
  }
}
