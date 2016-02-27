using MFroehlich.Parsing.JSON;
using RiotClient.Riot.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public class Invitation {
    private InvitationRequest invite;

    internal Invitation(InvitationRequest invite) {
      this.invite = invite;
    }

    internal async Task<LobbyStatus> Join() => await RiotServices.GameInvitationService.Accept(invite.InvitationId);

    public Lobby Accept() {
      var metadata = JSONParser.ParseObject(invite.GameMetaData);

      if ((int) metadata["gameTypeConfigId"] == GameConfig.CapDraft.Key) {
        return TBDLobby.Join(this, (int) metadata["queueId"]);
      } else {
        switch ((string) metadata["gameType"]) {
          case "PRACTICE_GAME":
            return CustomLobby.Join(this);
          case "NORMAL_GAME":
            return QueueLobby.Join(this, (int) metadata["queueId"]);
          default:
            throw new Exception("Lobby type not found: " + invite.InviteType);
        }
      }
    }

    public void Decline() {
      RiotServices.GameInvitationService.Decline(invite.InvitationId);
    }
  }
}
