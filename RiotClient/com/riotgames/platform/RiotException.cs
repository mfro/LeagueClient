using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  public class RiotException {
    [SerializedName("message")]
    public String Message { get; set; }

    [SerializedName("suppressed")]
    public Object[] Suppressed { get; set; }

    [SerializedName("rootCauseClassname")]
    public String rootCauseClassname { get; set; }

    [SerializedName("localizedMessage")]
    public String LocalizedMessage { get; set; }

    [SerializedName("cause")]
    public object Cause { get; set; }

    [SerializedName("substitutionArguments")]
    public Object[] SubstitutionArguments { get; set; }

    [SerializedName("errorCode")]
    public String ErrorCode { get; set; }
  }
}
