using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.login.LoginFailedException")]
  public class LoginFailedException : RiotException {

    [SerializedName("bannedUntilDate")]
    public Double BannedUntilDate { get; set; }
  }
}
