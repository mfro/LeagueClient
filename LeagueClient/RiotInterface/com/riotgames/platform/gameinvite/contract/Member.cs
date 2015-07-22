using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace LeagueClient.RiotInterface.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.Member")]
  public class Member {
    [SerializedName("hasDelegatedInvitePower")]
    public bool HasInvitePower { get; set; }

    [SerializedName("summonerName")]
    public String SummonerName { get; set; }

    [SerializedName("summonerId")]
    public Double SummonerId { get; set; }
  }
}
