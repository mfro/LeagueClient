using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.serviceproxy.dispatch.LcdsServiceProxyResponse")]
  public class LcdsServiceProxyResponse {
    [SerializedName("status")]
    public System.String status { get; set; }
    [SerializedName("payload")]
    public System.String payload { get; set; }
    [SerializedName("messageId")]
    public System.String messageId { get; set; }
    [SerializedName("methodName")]
    public System.String methodName { get; set; }
    [SerializedName("serviceName")]
    public System.String serviceName { get; set; }
  }
}
