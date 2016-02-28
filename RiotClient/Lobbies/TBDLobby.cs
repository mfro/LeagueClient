using MFroehlich.Parsing.JSON;
using RiotClient.Chat;
using RiotClient.com.riotgames.other;
using RiotClient.Riot.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;

namespace RiotClient.Lobbies {
  public class TBDLobby : QueueLobby {
    protected TBDGroupData GroupData { get; private set; }

    protected TBDLobby(int queueId) : base(queueId) { }

    public static new TBDLobby CreateLobby(int queueId) {
      var lobby = new TBDLobby(queueId);
      Session.Current.CurrentLobby = lobby;

      var guid = RiotServices.TeambuilderDraftService.CreateDraftPremade(queueId);
      RiotServices.AddHandler(guid, lcds => {
        lobby.GotGameData(lcds);
        var task = RiotServices.GameInvitationService.CreateGroupFinderLobby(queueId, lobby.GroupData.Phase.PremadeID.ToString());
        task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      });

      return lobby;
    }

    public static new TBDLobby Join(Invitation invite, int queueId) {
      var lobby = new TBDLobby(queueId);
      Session.Current.CurrentLobby = lobby;
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public virtual void SelectRoles(TBDRole one, TBDRole two) {
      RiotServices.TeambuilderDraftService.SpecifyDraftPositionPreferences(one.Key, two.Key);
      var me = Members[GroupData.Phase.MySlot] as TBDLobbyMember;
      me.PrimaryRole = one;
      me.SecondaryRole = two;
      me.Update();
    }

    public virtual void GotGameData(LcdsServiceProxyResponse lcds) {
      var json = JSONParser.ParseObject(lcds.payload);
      GroupData = JSONDeserializer.Deserialize<TBDGroupData>(json);

      if (lobbyStatus != null) UpdateSlots();
    }

    protected override void GotLobbyStatus(LobbyStatus status) {
      lobbyStatus = status;

      if (ChatLobby == null) {
        ChatLobby = new GroupChat(RiotChat.GetLobbyRoom(status.InvitationID, status.ChatKey), status.ChatKey);
      }

      foreach (var raw in status.InvitedPlayers) {
        if (!Invitees.ContainsKey(raw.SummonerId)) {
          var invitee = new LobbyInvitee(raw, this);
          Invitees.Add(invitee.SummonerID, invitee);
          OnMemberJoined(invitee);
        }
      }

      if (GroupData != null) UpdateSlots();

      if (!loaded) OnLoaded();
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

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      var lcds = args.Body as LcdsServiceProxyResponse;

      if (lcds != null) {
        switch (lcds.methodName) {
          case "tbdGameDtoV1":
            GotGameData(lcds);
            break;
          case "removedFromServiceV1":
            OnLeftLobby();
            break;
        }
      }

      return base.HandleMessage(args);
    }

    protected virtual void UpdateSlots() {
      var left = new List<QueueLobbyMember>(Members);

      foreach (var slot in GroupData.Phase.Slots.OrderBy(s => s.SlotId)) {
        var raw = lobbyStatus.Members[slot.SlotId];

        if (raw.SummonerName != slot.SummonerName) {
          Session.Log($"Difference between slot and member {raw.SummonerName} != {slot.SummonerName}");
        }

        if (Members.Count >= slot.SlotId) {
          var member = new TBDLobbyMember(raw, slot, this); ;
          Members.Add(member);
          OnMemberJoined(member);
        } else {
          Members[slot.SlotId].Update(raw);
          ((TBDLobbyMember) Members[slot.SlotId]).Update(slot);
          left.Remove(Members[slot.SlotId]);
        }
      }


      foreach (var member in left) {
        Members.Remove(member);
        OnMemberLeft(member);
      }
    }
  }

  public class TBDLobbyMember : QueueLobbyMember {
    private TBDSlotData data;

    public int SlotID => data.SlotId;
    public TBDRole PrimaryRole {
      get { return TBDRole.Values[data.Positions[0]]; }
      internal set { data.Positions[0] = value.Key; }
    }
    public TBDRole SecondaryRole {
      get { return TBDRole.Values[data.Positions[1]]; }
      internal set { data.Positions[1] = value.Key; }
    }

    internal TBDLobbyMember(Riot.Platform.Member member, TBDSlotData data, TBDLobby lobby) : base(member, lobby) {
      this.data = data;
    }

    internal void Update(TBDSlotData data) {
      this.data = data;
      OnChange();
    }

    internal void Update() => OnChange();
  }
}
