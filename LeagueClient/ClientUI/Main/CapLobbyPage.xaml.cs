using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using jabber.connection;
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

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderLobbyPage.xaml
  /// </summary>
  public partial class CapLobbyPage : Page, IClientSubPage {
    public LobbyStatus Status { get; private set; }
    public string GroupId { get; private set; }
    private CapPlayer[] players = new CapPlayer[5];
    private Dictionary<int, CapPlayer> found = new Dictionary<int, CapPlayer>();

    private CapPlayer me;
    private CapMePlayer meControl;
    private Room chatRoom;
    private CapLobbyState state;

    public bool IsCaptain { get { return me.SlotId == 0; } }
    public bool CanInvite { get; private set; }
    public event EventHandler Close;

    public CapLobbyPage(bool isCreating) {
      InitializeComponent();
      me = new CapPlayer(isCreating ? 0 : -1);
      me.Status = Logic.Cap.CapStatus.Choosing;
      me.PropertyChanged += Me_PropertyChanged;
      meControl = new CapMePlayer(me);
      FindAnotherButt.Visibility = Visibility.Collapsed;
      state = CapLobbyState.Inviting;

      SharedInit();
      CanInvite = isCreating;
    }

    public CapLobbyPage(CapPlayer solo) {
      InitializeComponent();
      me = solo;
      me.Status = Logic.Cap.CapStatus.Present;
      meControl = new CapMePlayer(me) { Editable = false };
      state = CapLobbyState.Searching;

      SharedInit();
    }

    private void SharedInit() {
      Client.MessageReceived += MessageReceived;

      ReadyButt.Visibility = Visibility.Collapsed;
      meControl.ChampClicked += Champion_Click;
      meControl.Spell1Clicked += Spell1_Click;
      meControl.Spell2Clicked += Spell2_Click;
      meControl.MasteryClicked += Player_MasteryClicked;

      Popup.ChampSelector.SkinSelected += ChampSelector_SkinSelected;
      Popup.SpellSelector.SpellSelected += Spell_Select;
      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains("CLASSIC")
                                    select spell);

      Client.ChatManager.UpdateStatus(LeagueStatus.InTeamBuilder);
    }

    public void GotLobbyStatus(LobbyStatus status) {
      Status = status;
      if (GroupId != null) JoinChat();
    }

    private void TextBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter) SendMessage();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      SendMessage();
    }

    private void JoinChat() {
      if (chatRoom != null) return;
      chatRoom = Client.ChatManager.GetTeambuilderRoom(GroupId, Status.ChatKey);
      chatRoom.OnJoin += room => Dispatcher.Invoke(() => ShowLobbyMessage("Joined chat lobby"));
      chatRoom.OnParticipantJoin += (s, e) => Dispatcher.Invoke(() => ShowLobbyMessage(e.Nick + " has joined the lobby"));
      chatRoom.OnRoomMessage += (s, e) => Dispatcher.Invoke(() => ShowMessage(chatRoom.Participants[e.From].Nick, e.Body));
      chatRoom.Join(Status.ChatKey);
    }

    private void SendMessage() {
      if (string.IsNullOrWhiteSpace(SendBox.Text)) return;
      chatRoom.PublicMessage(SendBox.Text);
      ShowMessage(Client.LoginPacket.AllSummonerData.Summoner.Name, SendBox.Text);
      SendBox.Text = "";
    }

    private void ShowLobbyMessage(string message) {
      var tr = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
      ChatScroller.ScrollToBottom();
    }

    private void ShowMessage(string user, string message) {
      var tr = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);
      tr.Text = user + ": ";
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.CornflowerBlue);
      tr = new TextRange(ChatHistory.Document.ContentEnd, ChatHistory.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, App.FontBrush);
      ChatScroller.ScrollToBottom();
    }

    public void GotGroupData(CapGroupData data) {
      GroupId = data.GroupId;
      me.SlotId = data.SlotId;
      if (Status != null) JoinChat();

      Dispatcher.Invoke(() => GameMap.Players.Clear());
      if(data.Slots != null) {
        for (int i = 0; i < data.Slots.Count; i++) {
          if (i == me.SlotId) {
            players[i] = me;
            Dispatcher.Invoke(() => GameMap.Players.Add(me));
            continue;
          }
          var player = data.Slots.Where(d => d.SlotId == i).FirstOrDefault();
          var capp = new CapPlayer(i);
          SetPlayerInfo(player, capp);
          players[i] = capp;
          Dispatcher.Invoke(() => GameMap.Players.Add(capp));
        }
      } else {
        players[me.SlotId] = me;
        Dispatcher.Invoke(() => GameMap.Players.Add(me));
      }

      if (data.InitialChampId > 0) me.Champion = LeagueData.GetChampData(data.InitialChampId);
      if (!data.InitialPosition?.Equals(Position.UNSELECTED.Key) ?? false)
        me.Position = Position.Values[data.InitialPosition];
      if (!data.InitialRole?.Equals(Role.UNSELECTED.Key) ?? false)
        me.Role = Role.Values[data.InitialRole];

      Dispatcher.Invoke(UpdateList);
    }

    public void UpdateList() {
      meControl.Editable = state == CapLobbyState.Inviting || state == CapLobbyState.Selecting;
      bool canReady = true;
      var list = new List<Control>();
      for(int i = 0; i < players.Length; i++) {
        var player = (found.ContainsKey(i)) ? found[i] : players[i];
        if (player == null) continue;
        Control c;
        if (player.SlotId == me.SlotId) c = meControl;
        else c = new CapOtherPlayer(player);
        c.Margin = new Thickness(0, 0, 0, 4);
        list.Add(c);
        if (players[i].Status != CapStatus.Present && players[i].Status != CapStatus.Ready) canReady = false;
      }
      if (IsCaptain) ReadyButt.Content = "Start Matchmaking";
      ReadyButt.Visibility = canReady ? Visibility.Visible : Visibility.Collapsed;
      InviteButt.Visibility = CanInvite ? Visibility.Visible : Visibility.Collapsed;
      SoloSearchButt.Visibility = IsCaptain && (state == CapLobbyState.Inviting || state == CapLobbyState.Selecting) ? Visibility.Visible : Visibility.Collapsed;
      PlayerList.ItemsSource = list;
    }

    private void MessageReceived(object sender, MessageHandlerArgs e) {
      if (e.Handled) return;
      LobbyStatus status;
      LcdsServiceProxyResponse response;
      InvitationRequest invite;
      InvitePrivileges privelage;
      RemovedFromLobbyNotification removed;
      if ((status = e.InnerEvent.Body as LobbyStatus) != null) {
        GotLobbyStatus(status);
        e.Handled = true;
      } else if ((response = e.InnerEvent.Body as LcdsServiceProxyResponse) != null) {
        if (response.status.Equals("OK")) {
          JSONObject json = null;
          CapSlotData slot = null;
          if(response.payload != null) {
            slot = (CapSlotData) (dynamic) (json = JSON.ParseObject(response.payload));
          }
          Console.WriteLine(response.methodName + ": " + response.payload);
          try {
            #region Massive Lcds Switch
            switch (response.methodName) {
              case "groupUpdatedV3":
                GotGroupData((CapGroupData) (dynamic) json);
                break;
              case "candidateAcceptedV1":
                found.Remove(slot.SlotId);
                break;
              case "candidateDeclinedV2":
                found.Remove(slot.SlotId);
                DoTimeout(ref players[slot.SlotId], (int) json["penaltyInSeconds"]);
                Dispatcher.Invoke(UpdateList);
                break;
              case "candidateDeclinedGroupV2":
                found.Remove(slot.SlotId);
                Dispatcher.Invoke(UpdateList);
                break;
              case "candidateFoundV2":
                var player = new CapPlayer(slot.SlotId);
                player.Champion = LeagueData.GetChampData(slot.ChampionId);
                player.Role = Role.Values[slot.Role];
                player.Position = players[slot.SlotId].Position;
                player.TimeoutEnd = DateTime.Now.Add(TimeSpan.FromSeconds((int) json["autoDeclineCandidateTimeout"]));
                player.Status = CapStatus.Found;
                found[player.SlotId] = player;
                Dispatcher.Invoke(UpdateList);
                break;
              case "candidateJoinedV1":
              case "estimatedWaitTimeRetrievedV1":
              case "featureTogglesRetrieved":
              case "gameStartFailedV1":
              case "gatekeeperRestrictedV1": break;
              case "groupCreatedV3":
                GotGroupData((CapGroupData) (dynamic) json);
                Task<LobbyStatus> task = RiotCalls.GameInvitationService.CreateGroupFinderLobby(61, GroupId);
                task.ContinueWith(t => GotLobbyStatus(t.Result));
                break;
              case "groupNoLongerAvailableV1":
              case "infoRetrievedV1":
              case "infoRetrievedV2":
              case "lastSelectedSkinForChampionUpdatedV1":
              case "leaverBusterLowPriorityQueueAbandonedV1":
              case "matchMadeV1":
              case "matchmakingPhaseStartedV1":
                state = CapLobbyState.Matching;
                break;
              case "quitDeniedV1": break;
              case "readinessIndicatedV1":
                if ((bool) json["ready"]) players[slot.SlotId].Status = CapStatus.Ready;
                else players[slot.SlotId].Status = CapStatus.Present;
                break;
              case "slotPopulatedV1":
                slot.Status = "POPULATED";
                if (players[slot.SlotId] == null) players[slot.SlotId] = new CapPlayer(slot.SlotId);
                SetPlayerInfo(slot, players[slot.SlotId]);
                Dispatcher.Invoke(UpdateList);
                break;
              case "championPickedV1":
                players[slot.SlotId].Champion = LeagueData.GetChampData(slot.ChampionId);
                Dispatcher.Invoke(UpdateList);
                break;
              case "spellsPickedV1":
                if (slot.Spell1Id > 0) players[slot.SlotId].Spell1 = LeagueData.GetSpellData(slot.Spell1Id);
                if (slot.Spell2Id > 0) players[slot.SlotId].Spell2 = LeagueData.GetSpellData(slot.Spell2Id);
                break;
              case "roleSpecifiedV1":
                players[slot.SlotId].Role = Role.Values[slot.Role];
                Dispatcher.Invoke(UpdateList);
                break;
              case "positionSpecifiedV1":
                players[slot.SlotId].Position = Position.Values[slot.Position];
                Dispatcher.Invoke(UpdateList);
                break;
              case "advertisedRoleSpecifiedV1":
                if (players[slot.SlotId] == null)
                  players[slot.SlotId] = new CapPlayer(slot.SlotId) { Status = CapStatus.ChoosingAdvert };
                players[slot.SlotId].Status = CapStatus.ChoosingAdvert;
                players[slot.SlotId].Role = Role.Values[slot.AdvertisedRole];
                Dispatcher.Invoke(UpdateList);
                break;
              case "advertisedPositionSpecifiedV1":
                if (players[slot.SlotId] == null)
                  players[slot.SlotId] = new CapPlayer(slot.SlotId) { Status = CapStatus.ChoosingAdvert };
                players[slot.SlotId].Status = CapStatus.ChoosingAdvert;
                players[slot.SlotId].Position = Position.Values[slot.AdvertisedPosition];
                Dispatcher.Invoke(UpdateList);
                break;
              case "playerRemovedV3":
                DoTimeout(ref players[slot.SlotId], (int) json["penaltyInSeconds"]);
                break;
              case "soloSearchedForAnotherGroupV2":
                if (slot.SlotId == me.SlotId) {
                  Dispatcher.Invoke(() => Client.QueueManager.ShowQueuer(new CapSoloQueuer(me)));
                  Client.MessageReceived -= MessageReceived;
                  if (Close != null) Close(this, new EventArgs());
                  if (json["reason"].Equals("KICKED"))
                    Client.QueueManager.ShowNotification(Alert.KickedFromCap);
                } else {
                  DoTimeout(ref players[slot.SlotId], (int) json["penaltyInSeconds"]);
                }
                break;
              case "removedFromServiceV1":
                if (state == CapLobbyState.Inviting)
                  RiotCalls.GameInvitationService.Leave();
                RiotCalls.CapService.Quit();
                Client.MessageReceived -= MessageReceived;
                Close?.Invoke(this, new EventArgs());
                if (json["reason"].Equals("GROUP_DISBANDED"))
                  Client.QueueManager.ShowNotification(Alert.GroupDisbanded);
                break;
              case "groupBuildingPhaseStartedV1":
                state = CapLobbyState.Searching;
                foreach(var cap in players) {
                  if (cap.Status == CapStatus.ChoosingAdvert) {
                    cap.Status = CapStatus.Searching;
                  }
                }
                Dispatcher.Invoke(UpdateList);
                break;
              case "soloSpecPhaseStartedV2":
                state = CapLobbyState.Selecting;
                for(int i = 0; i < players.Length; i++) {
                  if(players[i] == null) {
                    var cap = new CapPlayer(i);
                    cap.Role = Role.Values[json["initialSoloSpecRole"]];
                    cap.Position = Position.UNSELECTED;
                    cap.Status = CapStatus.ChoosingAdvert;
                    players[i] = cap;
                  }
                }
                Dispatcher.Invoke(UpdateList);
                break;
              default:
                Client.Log("Unhandled response to {0}, {1}", response.methodName, response.payload);
                break;
            }
            #endregion
          } catch (Exception x) {
            Client.Log(x);
          }
          e.Handled = true;
        } else if (!response.status.Equals("ACK")) Client.TryBreak(response.status + ": " + response.payload);
      } else if ((invite = e.InnerEvent.Body as InvitationRequest) != null) {

        e.Handled = true;
      } else if ((privelage = e.InnerEvent.Body as InvitePrivileges) != null) {
        CanInvite = privelage.canInvite;
        e.Handled = true;
      } else if((removed = e.InnerEvent.Body as RemovedFromLobbyNotification) != null) {
        switch (removed.removalReason) {
          case "PROGRESSED": break;
          default: break;
        }
        e.Handled = true;
      } else {
        Client.Log("In lobby recieved message type [{0}]", e.InnerEvent.Body?.GetType());
      }
    }

    private static void DoTimeout(ref CapPlayer player, int timeoutSecs) {
      player.ClearPlayerData();
      if (timeoutSecs > 0) {
        player.Status = CapStatus.Penalty;
        player.TimeoutEnd = DateTime.Now.Add(TimeSpan.FromSeconds(timeoutSecs));
      } else {
        player.Status = CapStatus.Searching;
      }
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
          if(player.ChampionId > 0) capp.Champion = LeagueData.GetChampData(player.ChampionId);
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
        default: Console.WriteLine(JSON.Stringify(player)); break;
      }
    }

    public bool CanPlay() => false;

    public Page GetPage() => this;

    private void FindAnotherButt_Click(object sender, RoutedEventArgs e) {
      var id = RiotCalls.CapService.SearchForAnotherGroup();
    }

    private void ReadyButt_Click(object sender, RoutedEventArgs e) {
      if (IsCaptain) {
      } else {
        bool ready = me.Status != CapStatus.Ready;
        RiotCalls.CapService.IndicateReadyness(ready);
      }
    }

    private void Me_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
      switch (e.PropertyName) {
        case nameof(CapPlayer.Role):
          RiotCalls.CapService.PickRole(me.Role, me.SlotId);
          break;
        case nameof(CapPlayer.Position):
          RiotCalls.CapService.PickPosition(me.Position, me.SlotId);
          break;
      }
    }

    #region Me Editing
    private bool spell1;

    private void Player_MasteryClicked(object src, EventArgs args) {
      Popup.MasteryEditor.Reset();
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Masteries;
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
      Popup.ChampSelector.UpdateChampList();
      Popup.BeginStoryboard(App.FadeIn);
      Popup.CurrentSelector = PopupSelector.Selector.Champions;
    }

    private void Popup_Close(object sender, EventArgs e) {
      Popup.BeginStoryboard(App.FadeOut);
      Popup.MasteryEditor.Save().Wait();
      meControl.UpdateBooks();
    }

    private void ChampSelector_SkinSelected(object sender, ChampionDto.SkinDto e) {
      if(me.Champion?.key  != Popup.ChampSelector.SelectedChampion.key) {
        me.Champion = Popup.ChampSelector.SelectedChampion;
        RiotCalls.CapService.PickChampion(Popup.ChampSelector.SelectedChampion.key);
      }
      if(e.num > 0)
        RiotCalls.CapService.PickSkin(e.id, false);
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
      RiotCalls.CapService.PickSpells(me.Spell1.key, me.Spell2.key);
      Popup.BeginStoryboard(App.FadeOut);
    }
    #endregion

    private void SoloSearchButt_Click(object sender, RoutedEventArgs e) {
      if (state == CapLobbyState.Inviting) {
        RiotCalls.CapService.SpecCandidates();

      } else
        RiotCalls.CapService.SearchForCandidates();
    }

    private void InviteButt_Click(object sender, RoutedEventArgs e) {
      InvitePopup.BeginStoryboard(App.FadeIn);
    }

    private void InvitePopup_Close(object sender, EventArgs e) {
      InvitePopup.BeginStoryboard(App.FadeOut);
      foreach(var user in InvitePopup.Users.Where(u => u.Value)) {
        double id;
        if (double.TryParse(user.Key.Replace("sum", ""), out id)) {
          RiotCalls.GameInvitationService.Invite(id);
        } else Client.TryBreak("Cannot parse user " + user.Key);
      }
    }

    public IQueuer HandleClose() {
      return new ReturnToLobbyQueuer(this);
    }

    public void ForceClose() {
      RiotCalls.GameInvitationService.Leave();
      RiotCalls.CapService.Quit();
    }
  }

  public enum CapLobbyState {
    Inviting, Selecting, Searching, Matching
  }
}