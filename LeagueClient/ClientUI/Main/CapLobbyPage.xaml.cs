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
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Cap;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MFroehlich.Parsing.DynamicJSON;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for TeambuilderLobbyPage.xaml
  /// </summary>
  public partial class CapLobbyPage : Page, IClientSubPage {
    public LobbyStatus Status { get; private set; }
    public JSONObject GroupData { get; private set; }
    public int MySlotId { get; private set; }
    private List<CapPlayer> players = new List<CapPlayer>();
    private Dictionary<int, CapPlayer> found = new Dictionary<int, CapPlayer>();

    private CapPlayer me;
    private CapMePlayer meControl;

    public event EventHandler Close;

    public CapLobbyPage() {
      InitializeComponent();
      me = new CapPlayer();
      me.Status = Logic.Cap.CapStatus.Choosing;
      meControl = new CapMePlayer(me);
      FindAnotherButt.Visibility = Visibility.Collapsed;

      SharedInit();
    }

    public CapLobbyPage(CapPlayer solo) {
      InitializeComponent();
      me = solo;
      me.Status = Logic.Cap.CapStatus.Present;
      meControl = new CapMePlayer(me) { Editable = false };

      SharedInit();
    }

    private void SharedInit() {
      Client.ChatManager.UpdateStatus(LeagueStatus.InTeamBuilder, StatusShow.Chat);
      Client.MessageReceived += MessageReceived;

      meControl.Spell1Clicked += Spell1_Click;
      meControl.Spell2Clicked += Spell2_Click;
      meControl.MasteryClicked += Player_MasteryClicked;

      Popup.ChampSelector.SkinSelected += ChampSelector_SkinSelected;
      Popup.SpellSelector.SpellSelected += Spell_Select;
      Popup.SpellSelector.Spells = (from spell in LeagueData.SpellData.Value.data.Values
                                    where spell.modes.Contains("CLASSIC")
                                    select spell);
    }

    public void GotLobbyStatus(LobbyStatus status) {
      Status = status;
    }

    public void GotGroupData(JSONObject data) {
      GroupData = data;
      MySlotId = (int) data["slotId"];

      players.Clear();
      Dispatcher.Invoke(() => GameMap.Players.Clear());
      for (int i = 0; i < data["slots"].Count; i++) {
        if(i == MySlotId) {
          me.SlotId = i;
          players.Add(me);
          Dispatcher.Invoke(() => GameMap.Players.Add(me));
          continue;
        }
        Func<dynamic, bool> check = d => d.slotId == i;
        var player = Enumerable.FirstOrDefault(Enumerable.Where((List<object>) data["slots"], check));
        var capp = new CapPlayer();
        SetPlayerInfo(player, capp);
        players.Add(capp);
        Dispatcher.Invoke(() => GameMap.Players.Add(capp));
      }

      Dispatcher.Invoke(UpdateList);
    }

    public void UpdateList() {
      bool canReady = true;
      var list = new List<Control>();
      for(int i = 0; i < players.Count; i++) {
        var player = (found.ContainsKey(i)) ? found[i] : players[i];
        Control c;
        if (player.SlotId == MySlotId) c = meControl;
        else c = new CapOtherPlayer(player);
        c.Margin = new Thickness(0, 0, 0, 4);
        list.Add(c);
        if (players[i].Status != CapStatus.Present && players[i].Status != CapStatus.Ready) canReady = false;
      }
      ReadyButt.Visibility = canReady ? Visibility.Visible : Visibility.Collapsed;
      PlayerList.ItemsSource = list;
    }

    private void MessageReceived(object sender, MessageHandlerArgs e) {
      if (e.Handled) return;
      var status = e.InnerEvent.Body as LobbyStatus;
      var response = e.InnerEvent.Body as LcdsServiceProxyResponse;
      if (status != null) {
        GotLobbyStatus(status);
      } else if (response != null) {
        if (response.status.Equals("OK")) {
          var json = JSON.ParseObject(response.payload);
          var data = json.Dictionary;
          int slot = (int) data["slotId"];
          Console.WriteLine(response.methodName + ": " + response.payload);
          try {
            switch (response.methodName) {
              case "groupCreatedV3":
                GotGroupData(json);
                Task<LobbyStatus> task = RiotCalls.GameInvitationService.CreateGroupFinderLobby(61, GroupData["groupId"]);
                task.ContinueWith(t => GotLobbyStatus(t.Result));
                break;
              case "groupUpdatedV3":
                GotGroupData(json);
                break;
              case "candidateFoundV2":
                var player = new CapPlayer();
                player.Champion = LeagueData.GetChampData((int) data["championId"]);
                player.Role = Role.Values[(string) data["role"]];
                player.Position = players[slot].Position;
                player.Timeout = new Duration(TimeSpan.FromSeconds((int) data["autoDeclineCandidateTimeout"]));
                player.SlotId = slot;
                player.Status = CapStatus.Found;
                found[player.SlotId] = player;
                Dispatcher.Invoke(UpdateList);
                break;
              case "candidateDeclinedGroupV2":
                found.Remove(slot);
                Dispatcher.Invoke(UpdateList);
                break;
              case "candidateDeclinedV2":
                found.Remove(slot);
                Dispatcher.Invoke(UpdateList);
                break;
              case "candidateAcceptedV1":
                found.Remove(slot);
                break;
              case "slotPopulatedV1":
                json["status"] = "POPULATED";
                SetPlayerInfo(json, players[slot]);
                Dispatcher.Invoke(UpdateList);
                break;
              case "readinessIndicatedV1":
                if ((bool) data["ready"]) players[slot].Status = CapStatus.Ready;
                else players[slot].Status = CapStatus.Present;
                break;
              case "spellsPickedV1":
                if (data.ContainsKey("spell1Id")) players[slot].Spell1 = LeagueData.GetSpellData((int) data["spell1Id"]);
                if (data.ContainsKey("spell2Id")) players[slot].Spell2 = LeagueData.GetSpellData((int) data["spell2Id"]);
                break;
              case "soloSearchedForAnotherGroupV2":
                if (slot == MySlotId) {
                  Dispatcher.Invoke(() => Client.QueueManager.ShowQueuer(new CapSoloQueuer(me)));
                  Client.MessageReceived -= MessageReceived;
                  if (Close != null) Close(this, new EventArgs());
                  if (data["reason"].Equals("KICKED"))
                    Client.QueueManager.ShowNotification(Alert.KickedFromCap());
                } else {
                  if (((int) data["penaltyInSeconds"]) > 0) {
                    players[slot].Status = CapStatus.Penalty;
                    players[slot].Timeout = new Duration(TimeSpan.FromSeconds((int) data["penaltyInSeconds"]));
                  } else {
                    players[slot].Status = CapStatus.Searching;
                  }
                }
                break;
              default:
                Client.Log("Unhandled response to {0}, {1}", response.methodName, response.payload);
                break;
            }
          } catch (Exception x) {
            Console.WriteLine(x);
          }
          e.Handled = true;
        } else if (!response.status.Equals("ACK")) Client.TryBreak(response.status + ": " + response.payload);
      } else { }
    }

    private void SetPlayerInfo(JSONObject player, CapPlayer capp) {
      switch ((string) player["status"]) {
        case "EMPTY":
          capp.Status = CapStatus.Searching;
          capp.Role = Role.Values[player["advertisedRole"]];
          capp.Position = Position.Values[player["position"]];
          capp.SlotId = player["slotId"];
          break;
        case "POPULATED":
          capp.Status = CapStatus.Present;
          capp.Role = Role.Values[player["role"]];
          capp.Position = Position.Values[player["position"]];
          if(player["championId"] > 0) capp.Champion = LeagueData.GetChampData(player["championId"]);
          capp.Spell1 = LeagueData.GetSpellData(player["spell1Id"]);
          capp.Spell2 = LeagueData.GetSpellData(player["spell2Id"]);
          capp.SlotId = player["slotId"];
          capp.Name = player["summonerName"];
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

    public bool CanPlay() {
      return false;
    }

    public Page GetPage() {
      return this;
    }

    private void FindAnotherButt_Click(object sender, RoutedEventArgs e) {
      var id = RiotCalls.CapService.SearchForAnotherGroup();
    }

    private void ReadyButt_Click(object sender, RoutedEventArgs e) {
      bool ready = me.Status != CapStatus.Ready;
      RiotCalls.CapService.IndicateReadyness(ready);
    }

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
      me.Champion = Popup.ChampSelector.SelectedChampion;
      RiotCalls.CapService.PickSkin(e.id, false);
      RiotCalls.CapService.UpdateLastSelectedSkin(e.id, Popup.ChampSelector.SelectedChampion.key);
      Popup_Close(sender, null);
    }

    private void Spell_Select(object sender, SpellDto spell) {
      if (spell1) me.Spell1 = spell;
      else me.Spell2 = spell;
      RiotCalls.CapService.PickSpells(me.Spell1.key, me.Spell2.key);
      Popup.BeginStoryboard(App.FadeOut);
    }
  }

  public class TeambuilderPlayer {
    public ChampionDto Champ { get; set; }
    public SpellDto Spell1 { get; set; }
    public SpellDto Spell2 { get; set; }
    public string Position { get; set; }
    public string Role { get; set; }
  }
}