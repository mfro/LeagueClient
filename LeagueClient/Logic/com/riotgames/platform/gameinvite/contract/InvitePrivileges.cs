using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace LeagueClient.Logic.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.InvitePrivileges")]
  public class InvitePrivileges {
    [SerializedName("canInvite")]
    public System.Boolean canInvite { get; set; }
  }
}
