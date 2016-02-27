using System;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.Invitee")]
  public class Invitee {
    [SerializedName("inviteeState")]
    public String InviteeState { get; set; }

    [SerializedName("summonerName")]
    public String SummonerName { get; set; }

    [SerializedName("summonerId")]
    public Int64 SummonerId { get; set; }

    [SerializedName("inviteeStateAsString")]
    public String InviteeStateString { get; set; }
  }
}
