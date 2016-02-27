using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.client.dynamic.configuration.ClientDynamicConfigurationNotification")]
  public class ClientDynamicConfigurationNotification {
    [SerializedName("configs")]
    public String Configs { get; set; }

    [SerializedName("delta")]
    public bool Delta { get; set; }
  }
}
