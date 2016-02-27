using RiotClient.Chat;
using RiotClient.com.riotgames.other;
using RiotClient.Riot;
using RiotClient.Riot.Platform;
using MFroehlich.Parsing.JSON;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public class TBDLobby : Lobby {
    public event EventHandler<RemovedFromServiceEventArgs> OnRemovedFromService;

    public LobbyStatus LobbyStatus { get; private set; }
    public TBDGroupData GroupData { get; private set; }
    public TBDLobbyMember[] Slots { get; } = new TBDLobbyMember[5];

    public override int QueueID { get; }

    private TBDLobby(int queueId) {
      QueueID = queueId;
    }

    public static TBDLobby Create(int queueId) {
      var lobby = new TBDLobby(queueId);
      var task = RiotServices.GameInvitationService.CreateArrangedTeamLobby(queueId);
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      RiotServices.TeambuilderDraftService.CreateDraftPremade(queueId);
      return lobby;
    }

    internal static TBDLobby Join(Invitation invite, int queueId) {
      var lobby = new TBDLobby(queueId);
      var task = invite.Join();
      task.ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      return lobby;
    }

    public override void EnterQueue() {
      if (!IsCaptain) return;

      RiotServices.TeambuilderDraftService.StartMatchmaking();
    }

    public override void Quit() {
      RiotServices.TeambuilderDraftService.QuitV2();
      base.Quit();
    }

    public void SetRole(int roleIndex, TBDRole role) {
      var data = GroupData.Phase.Slots[GroupData.Phase.MySlot];
      data.Positions[roleIndex] = role.Key;
      var me = Me as TBDLobbyMember;
      me.Update(data);
      RiotServices.TeambuilderDraftService.SpecifyDraftPositionPreferences(me.PrimaryRole.Key, me.SecondaryRole.Key);
    }

    internal override bool HandleMessage(MessageReceivedEventArgs args) {
      var lcds = args.Body as LcdsServiceProxyResponse;

      if (lcds != null) {
        if (lcds.status == "OK") {
          switch (lcds.methodName) {
            case "removedFromServiceV1":
              OnRemovedFromService.Invoke(this, new RemovedFromServiceEventArgs(lcds));
              return true;

            case "tbdGameDtoV1":
              GotGroupData(lcds);
              return true;
          }
        }
      }

      return base.HandleMessage(args);
    }

    protected override void GotLobbyStatus(LobbyStatus status) {
      base.GotLobbyStatus(status);

      if (chatID != null) {
        ChatLogin(RiotChat.GetLobbyRoom(lobbyStatus.InvitationID, lobbyStatus.ChatKey), lobbyStatus.ChatKey);
      }

      if (GroupData != null) UpdateSlots();
    }

    private void UpdateSlots() {
      var left = new List<long>(Members.Keys);

      foreach (var raw in GroupData.Phase.Slots) {
        if (Slots[raw.SlotId] == null && LobbyStatus.Members[raw.SlotId] != null) {
          Slots[raw.SlotId] = new TBDLobbyMember(LobbyStatus.Members[raw.SlotId], raw);
          base.OnMemberJoined(Slots[raw.SlotId]);
        } else {
          Slots[raw.SlotId].Update(raw);
          left.Remove(raw.SlotId);
        }
      }

      foreach (var id in left) {
        OnMemberLeft(Members[id]);
        Members.Remove(id);
      }

      if (!loaded) OnLoaded();
    }

    private void GotGroupData(LcdsServiceProxyResponse lcds) {
      var json = JSONParser.ParseObject(lcds.payload);
      GroupData = JSONDeserializer.Deserialize<TBDGroupData>(json);

      if (LobbyStatus != null) UpdateSlots();
    }

    public class TBDLobbyMember : LobbyMember {
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

      public TBDLobbyMember(Riot.Platform.Member member, TBDSlotData data) : base(member) {
        this.data = data;
      }

      public void Update(TBDSlotData data) {
        this.data = data;
        OnChange();
      }
    }

    public class RemovedFromServiceEventArgs {
      public string Reason { get; }
      public RemovedFromServiceEventArgs(LcdsServiceProxyResponse lcds) {
        JSONObject json = JSONParser.ParseObject(lcds.payload);
        Reason = json["reason"] as string;
      }
    }
  }
}
