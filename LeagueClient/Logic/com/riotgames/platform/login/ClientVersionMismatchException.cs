using System;
using System.Collections.Generic;
using RtmpSharp.IO;

namespace LeagueClient.Logic.Riot.Platform {

  [Serializable]
  [SerializedName("com.riotgames.platform.login.impl.ClientVersionMismatchException")]
  public class ClientVersionMismatchException : RiotException {

  }
}
