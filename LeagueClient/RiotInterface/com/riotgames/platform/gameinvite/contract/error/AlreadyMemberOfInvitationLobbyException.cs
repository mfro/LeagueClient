using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace LeagueClient.RiotInterface.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.error.AlreadyMemberOfInvitationLobbyException")]
  public class AlreadyMemberOfInvitationLobbyException : RiotException {
    [SerializedName("memberId")]
    public Double MemberId { get; set; }
  }
}
