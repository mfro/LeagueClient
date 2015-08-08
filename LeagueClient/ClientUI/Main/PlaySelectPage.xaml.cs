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

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for PlaySelectPage.xaml
  /// </summary>
  public partial class PlaySelectPage : Page, IClientSubPage {
    private static dynamic GameTypesRef = JSON.ParseObject(LeagueClient.Properties.Resources.Games);
    private static List<string> Order = new List<string> { "CLASSIC", "ODIN", "ARAM" };
    
    public BindingList<dynamic> GameGroups { get; private set; }
    public BindingList<dynamic> GameModes { get; private set; }
    public BindingList<dynamic> GameQueues { get; private set; }
    private dynamic selected;

    public event EventHandler Close;

    public PlaySelectPage() {
      GameGroups = new BindingList<dynamic>();
      GameModes = new BindingList<dynamic>();
      GameQueues = new BindingList<dynamic>();

      Func<dynamic, bool> GameCheck = g => g.Queues.Count == 0;
      Func<dynamic, bool> QueueCheck = q => !Client.AvailableQueues.ContainsKey(q["Id"]);
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

      foreach (var item in GameTypesRef.Games)
        if (item.Value.Games.Count > 0)
          GameGroups.Add(item.Value);

      InitializeComponent();
      GroupList.MouseUp += (src, e) => GroupSelected();
      ModeList.MouseUp += (src, e) => ModeSelected();
      QueueList.MouseUp += (src, e) => GameSelected();
    }

    void GameSelected() {
      selected = null;
      Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Collapsed;
      if (QueueList.SelectedIndex < 0) return;
      var info = (dynamic) QueueList.SelectedItem;
      QueueTitle.Text = info.Name;
      var config = Client.AvailableQueues[info.Id];
      var bots = "";
      if(info.ContainsKey("BotDifficulty")) bots = info.BotDifficulty;
      int type = 0;
      if (info.ContainsKey("Type")) type = info.Type;
      selected = new { Id = info.Id, Config = config, Bots = bots, Type = type };
      switch (type) {
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
        default:
          Join1.Visibility = Join2.Visibility = System.Windows.Visibility.Visible;
          Join1.Content = "Enter Soloqueue";
          Join2.Content = "Create Lobby";
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
      switch ((int) selected.Type) {
        case 0:
        case 1:
          var mmp = new MatchMakerParams();
          mmp.BotDifficulty = selected.Bots;
          mmp.QueueIds = new int[] { selected.Id };
          var search = await RiotCalls.MatchmakerService.AttachToQueue(mmp);
          if(search.PlayerJoinFailures?.Count > 0) {
            switch (search.PlayerJoinFailures[0].ReasonFailed) {
              case "QUEUE_DODGER":
                Client.QueueManager.ShowNotification(Alert.QueueDodger);
                Client.QueueManager.ShowQueuer(new BingeQueuer(search.PlayerJoinFailures[0].PenaltyRemainingTime));
                break;
            }
          } else {
            Client.QueueManager.ShowQueuer(new DefaultQueuer(mmp));
          }
          break;
        case 3:
          Client.QueueManager.ShowPage(new CapSoloPage());
          break;
      }
    }

    private void Join2_Click(object sender, RoutedEventArgs e) {
      if (Close != null) Close(this, new EventArgs());
      switch ((int) selected.Type) {
        case 0:
        case 1:
        case 2:
          //TODO
          //Client.QueueManager.CreateLobby(selected.Config, selected.Bots);
          break;
        case 3:
          Client.QueueManager.ShowPage(new CapLobbyPage());
          RiotCalls.CapService.CreateGroup();
          break;
      }
    }

    private void ListBox_Loaded(object sender, RoutedEventArgs e) {
      //var box = sender as ListBox;
      //var bd = box.Template.FindName("Bd", box) as Border;
      //bd.Padding = new Thickness(0);
    }

    public bool CanPlay() => false;

    public Page GetPage() => this;

    public void ForceClose() { }
    public IQueuer HandleClose() => null;
  }

  public class GameInfo {
    public string Name { get; private set; }
    public string Info { get; private set; }
    public GameInfo(string name, string info) {
      Name = name; Info = info;
    }
  }
}
