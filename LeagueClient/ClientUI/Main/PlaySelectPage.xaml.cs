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

      foreach (var item in GameTypesRef.Games)
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
      var config = (from qs in Client.AvailableQueues
                    where qs.Id == info.Id
                    select qs).FirstOrDefault();
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

    private void Join1_Click(object sender, RoutedEventArgs e) {
      if (Close != null) Close(this, new EventArgs());
      switch ((int) selected.Type) {
        case 0:
        case 1:
          var mmp = new MatchMakerParams();
          mmp.BotDifficulty = selected.Bots;
          mmp.QueueIds = new int[] { selected.Config.Id };
          RiotCalls.MatchmakerService.AttachToQueue(mmp);
          Client.QueueManager.ShowQueuer(new DefaultQueuer(selected.Config));
          break;
        case 3:
          Client.QueueManager.CreateCapSolo();
          break;
      }
    }

    private void Join2_Click(object sender, RoutedEventArgs e) {
      if (Close != null) Close(this, new EventArgs());
      switch ((int) selected.Type) {
        case 0:
        case 1:
        case 2:
          Client.QueueManager.CreateLobby(selected.Config, selected.Bots);
          break;
        case 3:
          Client.QueueManager.CreateCapLobby();
          break;
      }
    }

    private void ListBox_Loaded(object sender, RoutedEventArgs e) {
      var box = sender as ListBox;
      var bd = box.Template.FindName("Bd", box) as Border;
      bd.Padding = new Thickness(0);
    }

    public bool CanPlay() {
      return false;
    }

    public Page GetPage() {
      return this;
    }
  }

  public class GameInfo {
    public string Name { get; private set; }
    public string Info { get; private set; }
    public GameInfo(string name, string info) {
      Name = name; Info = info;
    }
  }
}
