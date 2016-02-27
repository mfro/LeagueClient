using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.gameinvite.contract.InvitationRequest")]
  public class InvitationRequest {

    [SerializedName("invitePayload")]
    public String InvitePayload { get; set; }

    [SerializedName("inviter")]
    public Inviter Inviter { get; set; }

    [SerializedName("inviteType")]
    public String InviteType { get; set; }

    [SerializedName("gameMetaData")]
    public String GameMetaData { get; set; }

    [SerializedName("owner")]
    public Player Owner { get; set; }

    [SerializedName("invitationState")]
    public String InvitationState { get; set; }

    [SerializedName("invitationId")]
    public String InvitationId { get; set; }
  }
}
