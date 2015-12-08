using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.com.riotgames.other;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.IO;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderLobbyPage.xaml
  /// </summary>
  public partial class CapLobbyPage : Page, IClientSubPage {

    #region Properties / Fields
    public LobbyStatus Status { get; private set; }
    public string GroupId { get; private set; }
    private CapPlayer[] players = new CapPlayer[5];
    private Dictionary<int, CapPlayer> found = new Dictionary<int, CapPlayer>();

    private CapMePlayer myControl;
    private CapPlayer me;
    private ChatRoomController chatRoom;
    private CapLobbyState state;
    private bool autoReady;

    public bool IsCaptain { get { return me.SlotId == 0; } }
    public bool CanInvite { get; private set; }
    public event EventHandler Close;
    #endregion

    #region Constructors

    public CapLobbyPage() {
      InitializeComponent();
    }

    public CapLobbyPage(bool isCreating) : this() {
      myControl = new CapMePlayer() { Margin = new Thickness(0, 10, 0, 0), VerticalAlignment = VerticalAlignment.Top };
      me = myControl.CapPlayer;
      FindAnotherButt.Visibility = Visibility.Collapsed;
      state = CapLobbyState.Inviting;

      SharedInit();
      CanInvite = isCreating;
    }


    public CapLobbyPage(CapPlayer solo) : this() {
      me = solo;
      myControl = new CapMePlayer(me) { Editable = false, Margin = new Thickness(0, 10, 0, 0), VerticalAlignment = VerticalAlignment.Top };
      state = CapLobbyState.Searching;

      SharedInit();
    }

    private void SharedInit() {
      MyGrid.Children.Add(myControl);
      me.PropertyChanged += Me_PropertyChanged;
      me.Status = CapStatus.Present;
      SoloSearchButt.Visibility = Visibility.Collapsed;
      ReadyButt.Visibility = Visibility.Collapsed;
      myControl.ChampClicked += Champion_Click;
      myControl.Spell1Clicked += Spell1_Click;
      myControl.Spell2Clicked += Spell2_Click;
      myControl.MasteryClicked += Player_MasteryClicked;
      myControl.RuneClicked += Player_RuneClicked;

      Popup.ChampSelector.SkinSelected += ChampSelector_SkinSelected;
      Popup.SpellSelector.SpellSelected += Spell_Select;
      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains("CLASSIC")
                                    select spell);

      PlayerList.Children.Clear();
      chatRoom = new ChatRoomController(SendBox, ChatHistory, SendButt, ChatScroller);
      Client.ChatManager.Status = ChatStatus.inTeamBuilder;
    }
    #endregion

    #region Message and Lobby handling
    private void JoinChat() {
      if (!chatRoom.IsJoined)
        chatRoom.JoinChat(RiotChat.GetTeambuilderRoom(GroupId, Status.ChatKey), Status.ChatKey);
    }

    public void GotLobbyStatus(LobbyStatus status) {
      Status = status;
      if (GroupId != null) JoinChat();

      if (status.Owner.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId)
        Client.CanInviteFriends = true;
      Dispatcher.Invoke(() => {
        InviteList.Children.Clear();
        foreach (var player in status.InvitedPlayers.Where(p => !p.InviteeState.Equals("CREATOR"))) {
          InviteList.Children.Add(new InvitedPlayer(player));
        }
      });
    }

    public void GotGroupData(CapGroupData data) {
      GroupId = data.GroupId;
      me.SlotId = data.SlotId;
      if (Status != null) JoinChat();

      if (data.Slots != null) {
        for (int i = 0; i < data.Slots.Count; i++) {
          if (i == me.SlotId) {
            players[i] = me;
            continue;
          }
          var player = data.Slots.FirstOrDefault(d => d.SlotId == i);
          var capp = new CapPlayer(i);
          SetPlayerInfo(player, capp);
          players[i] = capp;
        }
      } else {
        players[me.SlotId] = me;
      }

      if (data.InitialChampId > 0) me.Champion = LeagueData.GetChampData(data.InitialChampId);
      if (!data.InitialPosition?.Equals(Position.UNSELECTED.Key) ?? false)
        me.Position = Position.Values[data.InitialPosition];
      if (!data.InitialRole?.Equals(Role.UNSELECTED.Key) ?? false)
        me.Role = Role.Values[data.InitialRole];

      Dispatcher.Invoke(UpdateList);
    }

    public void UpdateList() {
      myControl.Editable = state == CapLobbyState.Inviting || state == CapLobbyState.Selecting;
      bool canReady = state == CapLobbyState.Searching;
      bool canSearch = state == CapLobbyState.Inviting;
      bool canMatch = true;

      PlayerList.Children.Clear();
      for (int i = 0; i < players.Length; i++) {
        var player = (found.ContainsKey(i)) ? found[i] : players[i];
        if (player == null) {
          canMatch = canReady = false;
          continue;
        }

        if (player.SlotId != me.SlotId) {
          var control = new CapOtherPlayer(player, IsCaptain);
          if (PlayerList.Children.Count < 3) control.Margin = new Thickness(0, 0, 10, 0);
          else control.Margin = new Thickness(0);
          control.CandidateReacted += Candidate_Reacted;
          control.Kicked += Candidate_Kicked;
          control.GiveInvite += Candidate_GiveInvite;

          PlayerList.Children.Add(control);
        }

        //Player other than captain is not ready - cannot start matchmaking
        if (i > 0 && players[i].Status != CapStatus.Ready) canMatch = false;
        if (players[i].Status != CapStatus.Present && players[i].Status != CapStatus.Ready) canReady = false;
        //Advertised role and position have not been selected
        if (players[i].Champion == null || players[i].Position == null || players[i].Position == Position.UNSELECTED || players[i].Role == null || players[i].Role == Role.UNSELECTED)
          canSearch = false;
      }
      if (IsCaptain) {
        AutoReadyBox.Content = "Auto Start Matchmaking";
        ReadyButt.Content = "Start Matchmaking";
      } else {
        ReadyButt.Content = me.Status == CapStatus.Ready ? "Not Ready" : "Ready";
      }
      ReadyButt.Visibility = canReady ? Visibility.Visible : Visibility.Collapsed;
      GameMap.UpdateList(players);

      if (!canReady) {
        foreach (var player in players.Where(p => p?.Status == CapStatus.Ready))
          player.Status = CapStatus.Present;
      }
      ReadyButt.IsEnabled = !autoReady;
      if (IsCaptain && canMatch && autoReady)
        RiotServices.CapService.StartMatchmaking();
      else if (canReady && autoReady)
        RiotServices.CapService.IndicateReadyness(true);

      //InviteButt.Visibility = CanInvite && state == CapLobbyState.Inviting ? Visibility.Visible : Visibility.Collapsed;

      if (IsCaptain && canSearch) SoloSearchButt.Visibility = Visibility.Visible;
      else if (state != CapLobbyState.Selecting) SoloSearchButt.Visibility = Visibility.Collapsed;
    }

    private static readonly Dictionary<string, string> MethodNames = new Dictionary<string, string> {
      ["candidateDeclinedGroupV2"] = "candidateDeclinedV2"
    };
    private Action<JSONObject> GetMethod(LcdsServiceProxyResponse response) {
      JSONObject json = null;
      if (response.payload != null) {
        json = JSON.ParseObject(response.payload);
      }
      Client.Log(response.methodName + ": " + response.payload);
      var name = response.methodName;
      if (MethodNames.ContainsKey(name)) name = MethodNames[name];
      var method = GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
      if (method != null)
        return (Action<JSONObject>) method.CreateDelegate(typeof(Action<JSONObject>), this);
      else throw new NotSupportedException(name);
    }

    #region LcdsMethods

    private void groupUpdatedV3(JSONObject json) => GotGroupData(json.To<CapGroupData>());

    private void candidateAcceptedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (found.ContainsKey(slot.SlotId))
        found[slot.SlotId].Status = CapStatus.Joining;
    }

    private void candidateDeclinedV2(JSONObject json) {
      var slot = json.To<CapSlotData>();
      found.Remove(slot.SlotId);
      DoTimeout(players[slot.SlotId], json["penaltyInSeconds"]);
      Dispatcher.Invoke(UpdateList);
    }

    private void candidateFoundV2(JSONObject json) {
      var slot = json.To<CapSlotData>();
      var player = new CapPlayer(slot.SlotId);
      player.Champion = LeagueData.GetChampData(slot.ChampionId);
      player.Role = Role.Values[slot.Role];
      player.Position = players[slot.SlotId].Position;
      player.TimeoutEnd = DateTime.Now.Add(TimeSpan.FromSeconds(json["autoDeclineCandidateTimeout"]));
      player.Status = CapStatus.Found;
      found[player.SlotId] = player;
      Dispatcher.Invoke(UpdateList);
    }

    private async void groupCreatedV3(JSONObject json) {
      GotGroupData(json.To<CapGroupData>());
      var status = await RiotServices.GameInvitationService.CreateGroupFinderLobby(61, GroupId);
      GotLobbyStatus(status);
    }

    private void matchMadeV1(JSONObject json) {
      Dispatcher.MyInvoke(chatRoom.ShowLobbyMessage, "Match Found");
    }

    private void matchmakingPhaseStartedV1(JSONObject json) {
      state = CapLobbyState.Matching;
      players[0].Status = CapStatus.Ready;
      Dispatcher.MyInvoke(chatRoom.ShowLobbyMessage, "Matchmaking Started");
    }

    private void readinessIndicatedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (json["ready"]) players[slot.SlotId].Status = CapStatus.Ready;
      else players[slot.SlotId].Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void slotPopulatedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      slot.Status = "POPULATED";
      if (found.ContainsKey(slot.SlotId)) found.Remove(slot.SlotId);
      if (players[slot.SlotId] == null) players[slot.SlotId] = new CapPlayer(slot.SlotId);
      SetPlayerInfo(slot, players[slot.SlotId]);
      if (state == CapLobbyState.Inviting)
        players[slot.SlotId].Status = CapStatus.Choosing;
      Dispatcher.Invoke(UpdateList);
    }

    private void skinPickedV1(JSONObject json) {
      Client.Log("Skin picked");
    }

    private void championPickedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      players[slot.SlotId].Champion = LeagueData.GetChampData(slot.ChampionId);
      Dispatcher.Invoke(UpdateList);
    }

    private void spellsPickedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (slot.Spell1Id > 0) players[slot.SlotId].Spell1 = LeagueData.GetSpellData(slot.Spell1Id);
      if (slot.Spell2Id > 0) players[slot.SlotId].Spell2 = LeagueData.GetSpellData(slot.Spell2Id);
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
    }

    private void roleSpecifiedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      players[slot.SlotId].Role = Role.Values[slot.Role];
      Dispatcher.Invoke(UpdateList);
    }

    private void positionSpecifiedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      players[slot.SlotId].Position = Position.Values[slot.Position];
      Dispatcher.Invoke(UpdateList);
    }

    private void advertisedRoleSpecifiedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (players[slot.SlotId] == null)
        players[slot.SlotId] = new CapPlayer(slot.SlotId) { Status = CapStatus.ChoosingAdvert };
      players[slot.SlotId].Status = CapStatus.ChoosingAdvert;
      players[slot.SlotId].Role = Role.Values[slot.AdvertisedRole];
      Dispatcher.Invoke(UpdateList);
    }

    private void advertisedPositionSpecifiedV1(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (players[slot.SlotId] == null)
        players[slot.SlotId] = new CapPlayer(slot.SlotId) { Status = CapStatus.ChoosingAdvert };
      players[slot.SlotId].Status = CapStatus.ChoosingAdvert;
      players[slot.SlotId].Position = Position.Values[slot.AdvertisedPosition];
      Dispatcher.Invoke(UpdateList);
    }

    private void playerRemovedV3(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (state == CapLobbyState.Inviting) {
        players[slot.SlotId] = null;
        Dispatcher.Invoke(UpdateList);
      } else
        DoTimeout(players[slot.SlotId], (int) json["penaltyInSeconds"]);
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
    }

    private void soloSearchedForAnotherGroupV2(JSONObject json) {
      var slot = json.To<CapSlotData>();
      if (slot.SlotId == me.SlotId) {
        Dispatcher.Invoke(() => Client.QueueManager.ShowPage(new CapSoloPage(me)));
        if (Close != null) Close(this, new EventArgs());
        if (json["reason"].Equals("KICKED"))
          Client.QueueManager.ShowNotification(AlertFactory.KickedFromCap());
      } else {
        DoTimeout(players[slot.SlotId], (int) json["penaltyInSeconds"]);
      }
      foreach (var cap in players)
        if (cap.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
    }

    private void removedFromServiceV1(JSONObject json) {
      ForceClose();
      Close?.Invoke(this, new EventArgs());
      if (json["reason"].Equals("GROUP_DISBANDED"))
        Client.QueueManager.ShowNotification(AlertFactory.GroupDisbanded());
    }

    private void groupBuildingPhaseStartedV1(JSONObject json) {
      state = CapLobbyState.Searching;
      foreach (var cap in players) {
        if (cap.Status == CapStatus.ChoosingAdvert) {
          cap.Status = CapStatus.Searching;
        }
      }
      Dispatcher.Invoke(UpdateList);
    }

    private void soloSpecPhaseStartedV2(JSONObject json) {
      state = CapLobbyState.Selecting;
      for (int i = 0; i < players.Length; i++) {
        if (players[i] == null) {
          var cap = new CapPlayer(i);
          cap.Role = Role.Values[json["initialSoloSpecRole"]];
          cap.Position = Position.UNSELECTED;
          cap.Status = CapStatus.ChoosingAdvert;
          players[i] = cap;
          if (IsCaptain) cap.PropertyChanged += Cap_PropertyChanged;
        } else players[i].Status = CapStatus.Present;
      }
      Dispatcher.Invoke(UpdateList);
    }

    #endregion

    public bool HandleMessage(MessageReceivedEventArgs e) {
      GameDTO game;
      LobbyStatus status;
      InvitePrivileges privelage;
      LcdsServiceProxyResponse response;
      RemovedFromLobbyNotification removed;
      PlayerCredentialsDto creds;


      if ((status = e.Body as LobbyStatus) != null) {
        GotLobbyStatus(status);
        return true;
      } else if ((response = e.Body as LcdsServiceProxyResponse) != null) {
        if (response.status.Equals("OK")) {
          JSONObject json = null;
          if (response.payload != null) json = JSON.ParseObject(response.payload);
          try {
            var method = GetMethod(response);
            method(json);
          } catch (Exception x) {
            Client.Log(x);
          }
          return true;
        } else if (!response.status.Equals("ACK")) Client.Log(response.status + ": " + response.payload);
      } else if ((privelage = e.Body as InvitePrivileges) != null) {
        Client.CanInviteFriends = privelage.canInvite;
        Dispatcher.Invoke(UpdateList);
        return true;
      } else if ((removed = e.Body as RemovedFromLobbyNotification) != null) {
        switch (removed.removalReason) {
          case "PROGRESSED": break;
          default: Client.Log("Unknown removal reason " + removed.removalReason); break;
        }
        return true;
      } else if ((creds = e.Body as PlayerCredentialsDto) != null) {
        Close?.Invoke(this, new EventArgs());
        Client.JoinGame(creds);
        return true;
      } else if ((game = e.Body as GameDTO) != null) {
        return true;
      }
      return false;
    }

    private void SetPlayerInfo(CapSlotData player, CapPlayer capp) {
      switch (player.Status) {
        case "EMPTY":
          capp.Status = CapStatus.Searching;
          capp.Role = Role.Values[player.AdvertisedRole];
          capp.Position = Position.Values[player.Position];
          capp.SlotId = player.SlotId;
          break;
        case "POPULATED":
          capp.Status = CapStatus.Present;
          capp.Role = Role.Values[player.Role];
          capp.Position = Position.Values[player.Position];
          if (player.ChampionId > 0) capp.Champion = LeagueData.GetChampData(player.ChampionId);
          capp.Spell1 = LeagueData.GetSpellData(player.Spell1Id);
          capp.Spell2 = LeagueData.GetSpellData(player.Spell2Id);
          capp.SlotId = player.SlotId;
          capp.Name = player.SummonerName;
          break;
        //case "CANDIDATE_ACCEPTED":
        //  capp.Status = CapStatus.Found;
        //  capp.Role = Role.Values[player["role"]];
        //  capp.Position = Position.Values[player["position"]];
        //  capp.Champion = LeagueData.GetChampData(player["championId"]);
        //break;
        default: break;
      }
    }
    #endregion

    private void Cap_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
      var cap = sender as CapPlayer;
      if (state == CapLobbyState.Selecting) {
        switch (e.PropertyName) {
          case nameof(cap.Position):
            RiotServices.CapService.SelectAdvertisedPosition(cap.Position, cap.SlotId);
            break;
          case nameof(cap.Role):
            RiotServices.CapService.SelectAdvertisedRole(cap.Role, cap.SlotId);
            break;
        }
      } else {
        cap.PropertyChanged -= Cap_PropertyChanged;
        return;
      }
      foreach (var player in players) {
        if (player?.Position == Position.UNSELECTED) return;
      }
      SoloSearchButt.Visibility = Visibility.Visible;
    }

    private void Candidate_Reacted(object sender, bool e) {
      if (e) {
        RiotServices.CapService.AcceptCandidate((sender as CapOtherPlayer).Player.SlotId);
      } else {
        RiotServices.CapService.DeclineCandidate((sender as CapOtherPlayer).Player.SlotId);
      }
    }

    private void Candidate_Kicked(object sender, EventArgs e) {
      var player = (CapOtherPlayer) sender;
      if (state == CapLobbyState.Searching) {
        RiotServices.CapService.KickPlayer(player.Player.SlotId);
      }
    }

    private void Candidate_GiveInvite(object sender, EventArgs e) {
      var player = (CapOtherPlayer) sender;
      var member = Status.Members.FirstOrDefault(m => m.SummonerName.Equals(player.Player.Name));
      if (member.HasInvitePower)
        RiotServices.GameInvitationService.RevokeInvitePrivileges(member.SummonerId);
      else
        RiotServices.GameInvitationService.GrantInvitePrivileges(member.SummonerId);
    }

    private static void DoTimeout(CapPlayer player, int timeoutSecs) {
      player.ClearPlayerData();
      if (timeoutSecs > 0) {
        player.Status = CapStatus.Penalty;
        player.TimeoutEnd = DateTime.Now.Add(TimeSpan.FromSeconds(timeoutSecs));
      } else {
        player.Status = CapStatus.Searching;
      }
    }

    #region Me Editing
    private void Me_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
      switch (e.PropertyName) {
        case nameof(CapPlayer.Role):
          RiotServices.CapService.PickRole(me.Role, me.SlotId);
          break;
        case nameof(CapPlayer.Position):
          RiotServices.CapService.PickPosition(me.Position, me.SlotId);
          break;
      }
    }

    private bool spell1;

    private void Player_MasteryClicked(object src, EventArgs args) {
      Popup.MasteryEditor.Reset();
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Masteries;
    }

    private void Player_RuneClicked(object sender, EventArgs e) {
      Popup.RuneEditor.Reset();
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Runes;
    }

    private void Spell1_Click(object src, EventArgs args) {
      spell1 = true;
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
    }

    private void Spell2_Click(object src, EventArgs args) {
      spell1 = false;
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Spells;
    }

    private void Champion_Click(object src, EventArgs args) {
      if (state != CapLobbyState.Inviting && state != CapLobbyState.Selecting) return;
      Popup.ChampSelector.UpdateChampList();
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Champions;
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
      Popup.MasteryEditor.Save().Wait();
      myControl.UpdateBooks();
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      if (me.Champion?.key != Popup.ChampSelector.SelectedChampion.key) {
        me.Champion = Popup.ChampSelector.SelectedChampion;
        var guid = RiotServices.CapService.PickChampion(Popup.ChampSelector.SelectedChampion.key);
        if (e.num > 0)
          RiotServices.AddHandler(guid, lcds => RiotServices.CapService.PickSkin(e.id, false));
      } else {
        if (e.num > 0)
          RiotServices.CapService.PickSkin(e.id, false);
      }
      Popup_Close(sender, null);
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) {
        me.Spell1 = spell;
        Client.LoginPacket.AllSummonerData.SummonerDefaultSpells.SummonerDefaultSpellMap["CLASSIC"].Spell1Id = spell.key;
      } else {
        me.Spell2 = spell;
        Client.LoginPacket.AllSummonerData.SummonerDefaultSpells.SummonerDefaultSpellMap["CLASSIC"].Spell2Id = spell.key;
      }
      RiotServices.CapService.PickSpells(me.Spell1.key, me.Spell2.key);
      Popup.BeginStoryboard(App.FadeOut);
    }
    #endregion

    #region Button Event Handlers

    private void ReadyButt_Click(object sender, RoutedEventArgs e) {
      if (IsCaptain) {
        RiotServices.CapService.StartMatchmaking();
      } else {
        bool ready = !me.Status.Equals(CapStatus.Ready);
        RiotServices.CapService.IndicateReadyness(ready);
      }
    }

    private void SoloSearchButt_Click(object sender, RoutedEventArgs e) {
      if (state == CapLobbyState.Inviting) {
        RiotServices.CapService.SpecCandidates();
        SoloSearchButt.Visibility = Visibility.Collapsed;
      } else
        RiotServices.CapService.SearchForCandidates();
    }

    private void QuitButt_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());

      ForceClose();
    }

    private void FindAnotherButt_Click(object sender, RoutedEventArgs e) => RiotServices.CapService.SearchForAnotherGroup();

    #endregion

    #region IClientSubPage
    public void ForceClose() {
      RiotServices.GameInvitationService.Leave();
      RiotServices.CapService.Quit();
      chatRoom?.LeaveChat();
      Client.ChatManager.Status = ChatStatus.outOfGame;
    }

    public Page Page => this;
    #endregion

    private void CheckBox_Checked(object sender, RoutedEventArgs e) {
      autoReady = AutoReadyBox.IsChecked ?? false;
      UpdateList();
    }
  }

  public enum CapLobbyState {
    Inviting, Selecting, Searching, Matching
  }
}