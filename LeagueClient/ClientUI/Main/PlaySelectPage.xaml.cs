using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for PlaySelectPage.xaml
  /// </summary>
  public partial class PlaySelectPage : Page, IClientSubPage {
    private static dynamic GameTypesRef = JSON.ParseObject(Properties.Resources.Games);
    
    public BindingList<dynamic> GameGroups { get; } = new BindingList<dynamic>();
    public BindingList<dynamic> GameModes { get; } = new BindingList<dynamic>();
    public BindingList<dynamic> GameQueues { get; } = new BindingList<dynamic>();
    private dynamic selected;

    public event EventHandler Close;

    public PlaySelectPage() {
      InitializeComponent();

      foreach (var item in GameTypesRef.Games)
        if (item.Value.Games.Count > 0)
          GameGroups.Add(item.Value);

      GroupList.MouseUp += (src, e) => GroupSelected();
      ModeList.MouseUp += (src, e) => ModeSelected();
      QueueList.MouseUp += (src, e) => GameSelected();
    }

    public static void Setup() {
      Func<dynamic, bool> GameCheck = g => g.Queues.Count == 0;
      Func<dynamic, bool> QueueCheck = q => !Client.AvailableQueues.ContainsKey(q["Id"]) && q["Id"] >= 0;
      foreach (var group in GameTypesRef.Games.Values) {
        foreach (var game in group.Games) {
          var queues = (IEnumerable<object>) Enumerable.Where(game.Queues.DynamicList, QueueCheck);
          foreach (var queue in queues.ToList())
            game.Queues.Remove(queue);
        }
        var games = (IEnumerable<object>) Enumerable.Where(group.Games.DynamicList, GameCheck);
        foreach (var item in games.ToList())
          group.Games.Remove(item);
      }
    }

    void GameSelected() {
      selected = null;
      Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Collapsed;
      if (QueueList.SelectedIndex < 0) return;
      var info = (dynamic) QueueList.SelectedItem;
      QueueTitle.Text = info.Name;
      GameQueueConfig config = null;
      if (info.Id >= 0) config = Client.AvailableQueues[info.Id];
      var bots = "";
      if(info.ContainsKey("BotDifficulty")) bots = info.BotDifficulty;
      int type = 0;
      if (info.ContainsKey("Type")) type = info.Type;
      selected = new { Id = info.Id, Config = config, Bots = bots, Type = type };
      switch (type) {
        case 3:
        case 0:
          Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Visible;
          Join1.Content = "Enter Soloqueue";
          Join2.Content = "Create Lobby";
          break;
        case 1:
          Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Visible;
          Join1.Content = "Enter Soloqueue";
          Join2.Content = "Invite Duo Partner";
          break;
        case 2:
          Join1.Visibility = System.Windows.Visibility.Collapsed;
          Join2.Visibility = System.Windows.Visibility.Visible;
          Join2.Content = "Create Lobby";
          break;
        case 4:
          Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Visible;
          Join1.Content = "Create Lobby";
          Join2.Content = "Join Lobby";
          break;
      }
    }
    
    void ModeSelected() {
      GameQueues.Clear();
      QueueTitle.Text = "";
      Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Collapsed;
      if (ModeList.SelectedIndex < 0) return;
      dynamic item = ModeList.SelectedItem;
      foreach (var queueId in item.Queues)
        GameQueues.Add(queueId);
      if (GameQueues.Count == 1) {
        QueueList.SelectedIndex = 0;
        GameSelected();
      }
      ModeTitle.Visibility = System.Windows.Visibility.Visible;
      ModeTitle.Text = $"{item.Name} - {item.Info}";
      ModeDetails.Text = GameTypesRef.Modes[item.Mode];
    }

    void GroupSelected() {
      GameModes.Clear();
      GameQueues.Clear();
      ModeTitle.Visibility = System.Windows.Visibility.Collapsed;
      QueueTitle.Text = "";
      ModeDetails.Text = "";
      Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Collapsed;
      if (GroupList.SelectedIndex < 0) return;
      foreach (var item in ((dynamic) GroupList.SelectedItem).Games)
        GameModes.Add(item);
      GroupDetails.Text = ((dynamic) GroupList.SelectedItem).Details;
    }

    private async void Join1_Click(object sender, RoutedEventArgs e) {
      if (Close != null) Close(this, new EventArgs());
      var mmp = new MatchMakerParams();
      mmp.BotDifficulty = selected.Bots;
      mmp.QueueIds = new int[] { selected.Id };
      switch ((int) selected.Type) {
        case 0://DEFAULT
        case 1://RANKED SOLO / DUO
          var search = await RiotCalls.MatchmakerService.AttachToQueue(mmp);
          if(search.PlayerJoinFailures?.Count > 0) {
            switch (search.PlayerJoinFailures[0].ReasonFailed) {
              case "QUEUE_DODGER":
                Client.QueueManager.ShowNotification(AlertFactory.QueueDodger());
                Client.QueueManager.ShowQueuer(new BingeQueuer(search.PlayerJoinFailures[0].PenaltyRemainingTime));
                break;
            }
          } else {
            Client.QueueManager.ShowQueuer(new DefaultQueuer(mmp));
          }
          break;
        case 3://TEAMBUILDER
          Client.QueueManager.ShowPage(new CapSoloPage());
          break;
        case 4://CUSTOM
          Client.QueueManager.ShowPage(new CustomCreatePage());
          break;
      }
    }

    private void Join2_Click(object sender, RoutedEventArgs e) {
      if (Close != null) Close(this, new EventArgs());
      var mmp = new MatchMakerParams();
      mmp.BotDifficulty = selected.Bots;
      mmp.QueueIds = new int[] { selected.Id };
      switch ((int) selected.Type) {
        case 0:
          var lobby = new DefaultLobbyPage(mmp);
          RiotCalls.GameInvitationService.CreateArrangedTeamLobby(mmp.QueueIds[0]).ContinueWith(t => Dispatcher.MyInvoke(lobby.GotLobbyStatus, t.Result));
          Client.QueueManager.ShowPage(lobby);
          break;
        case 1:
          //TODO Ranked duo game lobby
        case 2:
          //TODO Ranked team game lobby
          break;
        case 3:
          Client.QueueManager.ShowPage(new CapLobbyPage(true));
          RiotCalls.CapService.CreateGroup();
          break;
        case 4:
          //TODO Custom game list page
          //Client.QueueManager.ShowPage(new CustomListPage());
          break;
      }
    }

    public Page Page => this;
    public bool CanPlay => false;
    public bool CanClose => true;

    public void ForceClose() { }
    public IQueuer HandleClose() => null;

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }

  public class GameInfo {
    public string Name { get; private set; }
    public string Info { get; private set; }
    public GameInfo(string name, string info) {
      Name = name; Info = info;
    }
  }
}
