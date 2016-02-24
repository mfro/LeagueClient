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
using MFroehlich.Parsing.JSON;
using RtmpSharp.IO;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderLobbyPage.xaml
  /// </summary>
  public sealed partial class CapLobbyPage : Page, IClientSubPage {

    #region Properties / Fields
    public LobbyStatus Status { get; private set; }
    public string GroupId { get; private set; }
    private CapPlayer[] players = new CapPlayer[5];
    private Dictionary<int, CapPlayer> found = new Dictionary<int, CapPlayer>();

    private CapMePlayer myControl;
    private CapPlayer me;
    private ChatRoom chatRoom;
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
      if (isCreating)
        me = new CapPlayer(0);
      else
        me = new CapPlayer(-1) { Status = CapStatus.Choosing };

      var spells = Client.Session.LoginPacket.AllSummonerData.SummonerDefaultSpells.SummonerDefaultSpellMap["CLASSIC"];
      me.Spell1 = DataDragon.GetSpellData(spells.Spell1Id);
      me.Spell2 = DataDragon.GetSpellData(spells.Spell2Id);

      FindAnotherButt.Visibility = Visibility.Collapsed;
      state = CapLobbyState.Inviting;

      SharedInit();
      CanInvite = isCreating;

      if (isCreating)
        RiotServices.GameInvitationService.CreateGroupFinderLobby(61, GroupId).ContinueWith(t => GotLobbyStatus(t.Result));
    }


    public CapLobbyPage(CapPlayer solo) : this() {
      state = CapLobbyState.Searching;
      me = solo;

      SharedInit();
    }

    private void SharedInit() {
      me.Status = CapStatus.Present;
      SoloSearchButt.Visibility = Visibility.Collapsed;
      ReadyButt.Visibility = Visibility.Collapsed;
      me.CapEvent += PlayerHandler;

      Client.PopupSelector.SpellSelector.Spells = (from spell in DataDragon.SpellData.Value.data.Values
                                                   where spell.modes.Contains("CLASSIC")
                                                   select spell);

      PlayerList.Children.Clear();
      chatRoom = new ChatRoom(SendBox, ChatHistory, SendButt, ChatScroller);
      Client.Session.ChatManager.Status = ChatStatus.inTeamBuilder;
    }
    #endregion

    #region Message and Lobby handling
    private void JoinChat() {
      if (!chatRoom.IsJoined)
        chatRoom.JoinChat(RiotChat.GetTeambuilderRoom(GroupId));
    }

    public void GotLobbyStatus(LobbyStatus status) {
      Status = status;

      if (status.Owner.SummonerId == Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId)
        Client.Session.CanInviteFriends = true;
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
      JoinChat();

      if (data.Slots != null) {
        for (int i = 0; i < data.Slots.Count; i++) {
          if (i == me.SlotId) {
            players[i] = me;
            continue;
          }
          var player = data.Slots.FirstOrDefault(d => d.SlotId == i);
          var capp = new CapPlayer(i);
          capp.CapEvent += PlayerHandler;
          SetPlayerInfo(player, capp);
          players[i] = capp;
        }
      } else {
        players[me.SlotId] = me;
      }

      if (data.InitialChampId > 0) me.Champion = DataDragon.GetChampData(data.InitialChampId);
      if (!data.InitialPosition?.Equals(Position.UNSELECTED.Key) ?? false)
        me.Position = Position.Values[data.InitialPosition];
      if (!data.InitialRole?.Equals(Role.UNSELECTED.Key) ?? false)
        me.Role = Role.Values[data.InitialRole];

      Dispatcher.Invoke(UpdateList);
    }

    public void UpdateList() {
      LoadingGrid.Visibility = Visibility.Collapsed;
      myControl?.Dispose();
      bool canReady = state == CapLobbyState.Searching || state == CapLobbyState.Inviting;
      bool canSearch = state == CapLobbyState.Selecting || state == CapLobbyState.Inviting;
      bool canMatch = true;
      var editable = state == CapLobbyState.Inviting || state == CapLobbyState.Selecting ? CapMePlayer.CapControlState.Complete : CapMePlayer.CapControlState.None;

      PlayerList.Children.Clear();
      for (int i = 0; i < players.Length; i++) {
        var player = (found.ContainsKey(i)) ? found[i] : players[i];
        if (player == null) {
          canMatch = canReady = false;
          continue;
        }

        if (player.SlotId != me.SlotId) {
          var control = new CapOtherPlayer(player, IsCaptain);
          if (PlayerList.Children.Count < 3) control.Margin = new Thickness(0, 0, 20, 0);
          else control.Margin = new Thickness(0);

          PlayerList.Children.Add(control);
        } else {
          myControl = new CapMePlayer(me, editable);
          MyBorder.Child = myControl;
        }

        //Player other than captain is not ready - cannot start matchmaking
        if (i > 0 && players[i].Status != CapStatus.Ready) canMatch = false;
        if (players[i].Status != CapStatus.Present && players[i].Status != CapStatus.Ready) canReady = false;
        //Advertised role and position have not been selected
        if (players[i].Champion == null || players[i].Position == null || players[i].Position == Position.UNSELECTED || players[i].Role == null || players[i].Role == Role.UNSELECTED)
          canReady = false;

        bool isPlayer = players[i].Name != null;

        if (players[i].Position == null || players[i].Position == Position.UNSELECTED || players[i].Role == null || players[i].Role == Role.UNSELECTED)
          canSearch = canReady = false;
        if (isPlayer && players[i].Champion == null)
          canSearch = canReady = false;
      }

      if (!canReady) {
        foreach (var player in players.Where(p => p?.Status == CapStatus.Ready))
          player.Status = CapStatus.Present;
      }

      if (IsCaptain) {
        AutoReadyBox.Content = "Auto Start Matchmaking";
        ReadyButt.Content = "Start Matchmaking";
      } else {
        ReadyButt.Content = me.Status == CapStatus.Ready ? "Not Ready" : "Ready";
      }

      ReadyButt.Visibility = canReady ? Visibility.Visible : Visibility.Collapsed;
      GameMap.UpdateList(players);
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
        json = JSONParser.ParseObject(response.payload, 0);
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

    private void groupUpdatedV3(JSONObject json) => GotGroupData(JSONDeserializer.Deserialize<CapGroupData>(json));

    private void gameStartFailedV1(JSONObject json) {
      Close?.Invoke(this, new EventArgs());
      Dispose();
    }

    private void candidateAcceptedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if (found.ContainsKey(slot.SlotId))
        found[slot.SlotId].Status = CapStatus.Joining;
    }

    private void candidateDeclinedV2(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      found.Remove(slot.SlotId);
      DoTimeout(players[slot.SlotId], (int) json["penaltyInSeconds"]);
      Dispatcher.Invoke(UpdateList);
    }

    private void candidateFoundV2(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      var player = new CapPlayer(slot.SlotId);
      player.CapEvent += PlayerHandler;
      player.Champion = DataDragon.GetChampData(slot.ChampionId);
      player.Role = Role.Values[slot.Role];
      player.Position = players[slot.SlotId].Position;
      player.TimeoutEnd = DateTime.Now.Add(TimeSpan.FromSeconds((int) json["autoDeclineCandidateTimeout"]));
      player.Status = CapStatus.Found;
      found[player.SlotId] = player;
      Dispatcher.Invoke(UpdateList);
    }

    private void groupCreatedV3(JSONObject json) {
      GotGroupData(JSONDeserializer.Deserialize<CapGroupData>(json));
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
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if ((bool) json["ready"]) players[slot.SlotId].Status = CapStatus.Ready;
      else players[slot.SlotId].Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void slotPopulatedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      slot.Status = "POPULATED";
      if (found.ContainsKey(slot.SlotId)) found.Remove(slot.SlotId);
      if (players[slot.SlotId] == null) {
        var player = players[slot.SlotId] = new CapPlayer(slot.SlotId);
        player.CapEvent += PlayerHandler;
      }
      SetPlayerInfo(slot, players[slot.SlotId]);
      if (state == CapLobbyState.Inviting)
        players[slot.SlotId].Status = CapStatus.Choosing;
      Dispatcher.Invoke(UpdateList);
    }

    private void skinPickedV1(JSONObject json) {
      Client.Log("Skin picked");
    }

    private void championPickedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      players[slot.SlotId].Champion = DataDragon.GetChampData(slot.ChampionId);
      Dispatcher.Invoke(UpdateList);
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
    }

    private void spellsPickedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if (slot.Spell1Id > 0) players[slot.SlotId].Spell1 = DataDragon.GetSpellData(slot.Spell1Id);
      if (slot.Spell2Id > 0) players[slot.SlotId].Spell2 = DataDragon.GetSpellData(slot.Spell2Id);
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void roleSpecifiedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      players[slot.SlotId].Role = Role.Values[slot.Role];
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void positionSpecifiedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      players[slot.SlotId].Position = Position.Values[slot.Position];
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void advertisedRoleSpecifiedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if (players[slot.SlotId] == null) {
        var player = players[slot.SlotId] = new CapPlayer(slot.SlotId) { Status = CapStatus.ChoosingAdvert };
        player.CapEvent += PlayerHandler;
      }
      players[slot.SlotId].Status = CapStatus.ChoosingAdvert;
      players[slot.SlotId].Role = Role.Values[slot.AdvertisedRole];
      Dispatcher.Invoke(UpdateList);
    }

    private void advertisedPositionSpecifiedV1(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if (players[slot.SlotId] == null) {
        var player = players[slot.SlotId] = new CapPlayer(slot.SlotId) { Status = CapStatus.ChoosingAdvert };
        player.CapEvent += PlayerHandler;
      }
      players[slot.SlotId].Status = CapStatus.ChoosingAdvert;
      players[slot.SlotId].Position = Position.Values[slot.AdvertisedPosition];
      Dispatcher.Invoke(UpdateList);
    }

    private void playerRemovedV3(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if (state == CapLobbyState.Inviting) {
        players[slot.SlotId] = null;
        Dispatcher.Invoke(UpdateList);
      } else
        DoTimeout(players[slot.SlotId], (int) json["penaltyInSeconds"]);
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void soloSearchedForAnotherGroupV2(JSONObject json) {
      var slot = JSONDeserializer.Deserialize<CapSlotData>(json);
      if (slot.SlotId == me.SlotId) {
        Dispatcher.Invoke(() => Client.Session.QueueManager.ShowPage(new CapSoloPage(me)));
        if (Close != null) Close(this, new EventArgs());
        if (json["reason"].Equals("KICKED"))
          Client.Session.QueueManager.ShowNotification(AlertFactory.KickedFromCap());
      } else {
        DoTimeout(players[slot.SlotId], (int) json["penaltyInSeconds"]);
      }
      foreach (var cap in players)
        if (cap?.Status == CapStatus.Ready) cap.Status = CapStatus.Present;
      Dispatcher.Invoke(UpdateList);
    }

    private void removedFromServiceV1(JSONObject json) {
      Dispose();
      Close?.Invoke(this, new EventArgs());
      if (json["reason"].Equals("GROUP_DISBANDED"))
        Client.Session.QueueManager.ShowNotification(AlertFactory.GroupDisbanded());
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

      var team = new List<Tuple<Position, Role>> {
        Tuple.Create(Position.TOP, Role.ANY),
        Tuple.Create(Position.JUNGLE, Role.ANY),
        Tuple.Create(Position.MIDDLE, Role.ANY),
        Tuple.Create(Position.BOTTOM, Role.MARKSMAN),
        Tuple.Create(Position.BOTTOM, Role.SUPPORT),
      };

      foreach (var player in players)
        if (player != null)
          team.Remove(team.FirstOrDefault(pair => pair.Item1 == player.Position));

      for (int i = 0; i < players.Length; i++) {
        if (players[i] == null) {
          var pos = team[0];

          var cap = new CapPlayer(i);
          cap.Status = CapStatus.ChoosingAdvert;
          cap.CapEvent += PlayerHandler;

          RiotServices.CapService.SelectAdvertisedPosition(pos.Item1, i);
          RiotServices.CapService.SelectAdvertisedRole(pos.Item2, i);

          team.Remove(pos);
          players[i] = cap;
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
          if (!string.IsNullOrEmpty(response.payload)) json = JSONParser.ParseObject(response.payload, 0);
          try {
            var method = GetMethod(response);
            method(json);
          } catch (Exception x) {
            Client.Session.ThrowException(x);
          }
          return true;
        } else if (!response.status.Equals("ACK")) Client.Log(response.status + ": " + response.payload);
      } else if ((privelage = e.Body as InvitePrivileges) != null) {
        Client.Session.CanInviteFriends = privelage.canInvite;
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
        Client.Session.Credentials = creds;
        Client.Session.JoinGame();
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
          if (player.ChampionId > 0) capp.Champion = DataDragon.GetChampData(player.ChampionId);
          capp.Spell1 = DataDragon.GetSpellData(player.Spell1Id);
          capp.Spell2 = DataDragon.GetSpellData(player.Spell2Id);
          capp.SlotId = player.SlotId;
          capp.Name = player.SummonerName;
          break;
        //case "CANDIDATE_ACCEPTED":
        //  capp.Status = CapStatus.Found;
        //  capp.Role = Role.Values[player["role"]];
        //  capp.Position = Position.Values[player["position"]];
        //  capp.Champion = DataDragon.GetChampData(player["championId"]);
        //break;
        default: break;
      }
    }
    #endregion

    private void PlayerHandler(object sender, CapPlayerEventArgs e) {
      var player = sender as CapPlayer;

      var accepted = e as CandidateAcceptedEventArgs;
      var invite = e as GiveInviteEventArgs;
      var kick = e as KickedEventArgs;
      var change = e as PropertyChangedEventArgs;

      if (accepted != null) {
        if (accepted.Accepted) {
          RiotServices.CapService.AcceptCandidate(player.SlotId);
        } else {
          RiotServices.CapService.DeclineCandidate(player.SlotId);
        }
      } else if (invite != null) {
        var member = Status.Members.FirstOrDefault(m => m.SummonerName.Equals(player.Name));
        if (member.HasInvitePower)
          RiotServices.GameInvitationService.RevokeInvitePrivileges(member.SummonerId);
        else
          RiotServices.GameInvitationService.GrantInvitePrivileges(member.SummonerId);
      } else if (kick != null) {
        if (state == CapLobbyState.Searching) {
          RiotServices.CapService.KickPlayer(player.SlotId);
        }
      } else if (change != null) {
        switch (change.PropertyName) {
          case nameof(player.Position):
            if (player == me)
              RiotServices.CapService.PickPosition(change.Value as Position, player.SlotId);
            else
              RiotServices.CapService.SelectAdvertisedPosition(change.Value as Position, player.SlotId);
            break;
          case nameof(player.Role):
            if (player == me)
              RiotServices.CapService.PickRole(change.Value as Role, player.SlotId);
            else
              RiotServices.CapService.SelectAdvertisedRole(change.Value as Role, player.SlotId);
            break;
          case nameof(player.Champion):
            RiotServices.CapService.PickChampion((change.Value as ChampionDto).key);
            break;
          case nameof(player.Spell1):
            RiotServices.CapService.PickSpells((change.Value as SpellDto).key, player.Spell2.key);
            break;
          case nameof(player.Spell2):
            RiotServices.CapService.PickSpells(player.Spell1.key, (change.Value as SpellDto).key);
            break;
        }
      }
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

    #region UI Event Handlers

    private void Border_MouseEnter(object sender, MouseEventArgs e) {
      InviteBorder.BeginStoryboard(App.FadeIn);
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e) {
      InviteBorder.BeginStoryboard(App.FadeOut);
    }

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

      Dispose();
    }

    private void FindAnotherButt_Click(object sender, RoutedEventArgs e) => RiotServices.CapService.SearchForAnotherGroup();

    #endregion

    #region IClientSubPage
    public void Dispose() {
      RiotServices.GameInvitationService.Leave();
      RiotServices.CapService.Quit();
      chatRoom.Dispose();
      Client.Session.ChatManager.Status = ChatStatus.outOfGame;
      if (myControl != null) myControl.Dispose();
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