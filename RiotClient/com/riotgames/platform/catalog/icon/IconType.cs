using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.catalog.icon.IconType")]
  public class IconType {
    [SerializedName("iconTypeId")]
    public System.Int32 iconTypeId { get; set; }
    [SerializedName("name")]
    public System.String name { get; set; }
  }
}
