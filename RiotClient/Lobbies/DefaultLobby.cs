using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;
using RiotClient.Riot.Platform;
using RiotClient.Riot;
using RiotClient.Chat;

namespace RiotClient.Lobbies {
  public class DefaultLobby : Lobby {
    private MatchMakerParams mmp;

    public override int QueueID => mmp.QueueIds[0];

    private DefaultLobby() { }

    public static DefaultLobby Create(MatchMakerParams mmp) {
      var lobby = new DefaultLobby();
      lobby.mmp = mmp;
      var task = RiotServices.GameInvitationService.CreateArrangedTeamLobby(lobby.QueueID);
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    internal static DefaultLobby Join(Invitation invite, int queueId) {
      var lobby = new DefaultLobby();
      lobby.mmp = new MatchMakerParams { QueueIds = new[] { queueId } };
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public override void Quit() {
      if (queue != null) {
        RiotServices.MatchmakerService.CancelFromQueueIfPossible(Session.Current.Account.SummonerID);
        RiotServices.MatchmakerService.PurgeFromQueues();
      }
      base.Quit();
    }

    public override async void EnterQueue() {
      if (!IsCaptain) return;

      var search = await RiotServices.MatchmakerService.AttachTeamToQueue(mmp);
      OnQueueEntered(search);
    }

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      var notify = args.Body as GameNotification;
      var queue = args.Body as SearchingForMatchNotification;
      var msg = args.Body as SimpleDialogMessage;

      if (queue != null) {
        OnQueueEntered(queue);
        return true;
      } else if (notify != null) {
        OnQueueLeft();
        return true;
      } else if (msg != null) {
        if (msg.titleCode.Equals("ready_check.penalty.applied")) {
          //TODO Failed to accept notifcation
          return true;
        }
      }

      return base.HandleMessage(args);
    }

    protected override void GotLobbyStatus(LobbyStatus status) {
      mmp.InvitationId = status.InvitationID;
      base.GotLobbyStatus(status);

      if (chatID == null) {
        ChatLogin(RiotChat.GetLobbyRoom(lobbyStatus.InvitationID, lobbyStatus.ChatKey), lobbyStatus.ChatKey);
      }

      var left = new List<long>(Members.Keys);
      foreach (var raw in status.Members) {
        LobbyMember member;
        if (Members.TryGetValue(raw.SummonerId, out member)) {
          member.Update(raw);
          left.Remove(raw.SummonerId);
        } else {
          member = new DefaultLobbyMember(raw);
          OnMemberJoined(member);
        }
      }
      foreach (var id in left) {
        OnMemberLeft(Members[id]);
        Members.Remove(id);
      }

      if (!loaded) OnLoaded();
    }

    protected override void OnMemberJoined(LobbyMember member) {
      base.OnMemberJoined(member);
      mmp.Team = Members.Values.Select(m => (int) m.SummonerID).ToList();
    }

    protected override void OnMemberLeft(LobbyMember member) {
      base.OnMemberLeft(member);
      mmp.Team = Members.Values.Select(m => (int) m.SummonerID).ToList();
    }

    public class DefaultLobbyMember : LobbyMember {
      public DefaultLobbyMember(Member raw) : base(raw) {

      }
    }
  }
}
