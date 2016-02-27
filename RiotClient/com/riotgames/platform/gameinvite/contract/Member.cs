using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.Member")]
  public class Member {
    [SerializedName("hasDelegatedInvitePower")]
    public bool HasInvitePower { get; set; }

    [SerializedName("summonerName")]
    public String SummonerName { get; set; }

    [SerializedName("summonerId")]
    public Int64 SummonerId { get; set; }
  }
}
