using RiotClient.com.riotgames.other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public class TBDLobby : QueueLobby {
    protected TBDGroupData GroupData { get; private set; }
    protected TBDLobbyMember[] Slots { get; } = new TBDLobbyMember[5];

    protected TBDLobby(int queueId) : base(queueId) { }

    public static new TBDLobby CreateLobby(int queueId) {
      var lobby = new TBDLobby(queueId);
      Session.Current.CurrentLobby = lobby;
      var task = RiotServices.GameInvitationService.CreateArrangedTeamLobby(queueId);
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public static new TBDLobby Join(Invitation invite, int queueId) {
      var lobby = new TBDLobby(queueId);
      Session.Current.CurrentLobby = lobby;
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public override void StartQueue() {
      throw new NotImplementedException();
    }

    public override void CancelQueue() {
      throw new NotImplementedException();
    }

    public override void Dispose() {
      if (queue != null) RiotServices.TeambuilderDraftService.LeaveMatchmaking();
      RiotServices.TeambuilderDraftService.QuitV2();
      base.Dispose();
    }
  }

  public class TBDLobbyMember : QueueLobbyMember {
    private TBDSlotData data;

    public int SlotID => data.SlotId;
    public TBDRole PrimaryRole {
      get { return TBDRole.Values[data.Positions[0]]; }
      set { data.Positions[0] = value.Key; }
    }
    public TBDRole SecondaryRole {
      get { return TBDRole.Values[data.Positions[1]]; }
      set { data.Positions[1] = value.Key; }
    }

    internal TBDLobbyMember(Riot.Platform.Member member, TBDSlotData data, TBDLobby lobby) : base(member, lobby) {
      this.data = data;
    }

    internal void Update(TBDSlotData data) {
      this.data = data;
      OnChange();
    }
  }
}
