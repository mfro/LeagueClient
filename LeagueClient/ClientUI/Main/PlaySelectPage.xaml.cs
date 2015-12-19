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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using LeagueClient.Logic.Riot.Team;
using MFroehlich.Parsing.JSON;
using RtmpSharp.Messaging;
using System.Threading;
using System.Timers;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for PlaySelectPage.xaml
  /// </summary>
  public sealed partial class PlaySelectPage : UserControl, IDisposable {
    private static readonly Duration
      MoveDuration = new Duration(TimeSpan.FromMilliseconds(200)),
      ButtonDuration = new Duration(TimeSpan.FromMilliseconds(80));

    private static readonly AnimationTimeline
      MarginShrink = new ThicknessAnimation(new Thickness(MarginSize - BorderSize, MarginSize - BorderSize, -3, -3), MoveDuration),
      BorderExpand = new ThicknessAnimation(new Thickness(BorderSize), MoveDuration),
      MarginExpand = new ThicknessAnimation(new Thickness(MarginSize, MarginSize, 0, 0), MoveDuration),
      BorderShrink = new ThicknessAnimation(new Thickness(0), MoveDuration);

    private const int BorderSize = 3;
    private const int MarginSize = 10;

    public event EventHandler Close;

    //private Thread update;
    private Queue selected;
    private QueueController queue;
    private List<Queue> queues;
    //private Dictionary<ListBox, List<Queue>> queues = new Dictionary<ListBox, List<Queue>>();

    public PlaySelectPage() {
      InitializeComponent();

      #region Queues
      queues = new List<Queue> {
      //queues[ClassicQueues] = new List<Queue> {
        new Queue("Teambuilder", 61, "Enter Soloqueue", "Create Lobby", PlayTeambuilder),
        new Queue("Blind Pick 5v5", 2, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new Queue("Draft Pick 5v5", 14, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new Queue("Blind Pick 3v3", 8, "Enter Soloqueue", "Create Lobby", PlayStandard),
      //};

      //queues[SpecialQueues] = new List<Queue> {
        new Queue("Blind Pick Dominion", 16, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new Queue("Draft Pick Dominion", 17, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new Queue("ARAM", 65, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new Queue("King Poro", 300, "Enter Soloqueue", "Create Lobby", PlayStandard),
      //};

      //queues[RankedQueues] = new List<Queue> {
        new Queue("Ranked Solo / Duo Queue", 4, "Enter Soloqueue", "Invite Duo Partner", PlayRanked),
        new Queue("Ranked Teams 5v5", 42, null, "Create Lobby", PlayRankedTeams),
        new Queue("Ranked Teams 3v3", 41, null, "Create Lobby", PlayRankedTeams),
      };

      #endregion

      queue = new QueueController(QueueLabel, ChatStatus.inQueue, ChatStatus.outOfGame);
      SummonersRift.Tag = GameMap.SummonersRift;
      //update = new Thread(UpdateLoop) { IsBackground = true, Name = "PlaySelectUpdateLoop" };
      //update.Start();
    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      var game = args.Body as GameDTO;

      if (game != null && game.GameState.Equals("JOINING_CHAMP_SELECT")) {
        Dispatcher.Invoke(() => {
          var popup = new DefaultQueuePopup(game);
          popup.Close += (src, e) => SetInQueue(false);
          Client.QueueManager.ShowQueuePopup(popup);
        });
        return true;
      }

      return false;
    }

    private void SetInQueue(bool inQueue) {
      if (inQueue) {
        queue.Start();
        Dispatcher.Invoke(() => {
          CreateCustomButton.Visibility = JoinCustomButton.Visibility = QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Collapsed;
          QueueLabel.Visibility = CancelButton.Visibility = Visibility.Visible;
        });
      } else {
        queue.Cancel();
        Dispatcher.Invoke(() => {
          CreateCustomButton.Visibility = JoinCustomButton.Visibility = QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Visible;
          QueueLabel.Visibility = CancelButton.Visibility = Visibility.Collapsed;
        });
      }
      Queues.IsEnabled = !inQueue;
      //ClassicQueues.IsEnabled = SpecialQueues.IsEnabled = RankedQueues.IsEnabled = !inQueue;
    }

    //private void UpdateLoop() {
    //  while (true) {
    //    foreach (var category in queues.Values) {
    //      foreach (var item in category)
    //        RiotServices.MatchmakerService.GetQueueInformation(item.ID)
    //            .ContinueWith(task => item.Details = task.Result.QueueLength + " People in queue");
    //    }
    //    Thread.Sleep(30000);
    //  }
    //}

    private void QueueList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      var src = sender as ListBox;
      if (src.SelectedIndex < 0) return;
      var queue = (Queue) ((ListBox) sender).SelectedItem;

      //foreach (var item in queues.Keys)
      //  if (item != src) item.SelectedIndex = -1;

      if (queue.Action1 != null) {
        QueueButton1.Visibility = Visibility.Visible;
        QueueButton1.Content = queue.Action1;
      } else QueueButton1.Visibility = Visibility.Collapsed;

      if (queue.Action2 != null) {
        QueueButton2.Visibility = Visibility.Visible;
        QueueButton2.Content = queue.Action2;
      } else QueueButton2.Visibility = Visibility.Collapsed;
      selected = queue;
    }

    public void Reset() {
      var list = from item in queues
                 where Client.AvailableQueues.ContainsKey(item.ID)
                 select item;
      //((Grid) category.Key.Parent).Visibility = list.Count() > 0 ? Visibility.Visible : Visibility.Collapsed;
      Queues.ItemsSource = list;
    }

    public void Dispose() {
      //update.Abort();
      queue.Dispose();
    }

    #region Plays
    private void PlayTeambuilder(int button) {
      Close?.Invoke(this, new EventArgs());
      switch (button) {
        case 0: Client.QueueManager.ShowPage(new CapSoloPage()); break;
        case 1:
          Client.QueueManager.ShowPage(new CapLobbyPage(true));
          RiotServices.CapService.CreateGroup();
          break;
      }
    }

    private async void PlayStandard(int button) {
      Close?.Invoke(this, new EventArgs());
      var mmp = new MatchMakerParams { QueueIds = new[] { selected.ID } };
      switch (button) {
        case 0:
          var search = await RiotServices.MatchmakerService.AttachToQueue(mmp);
          if (Client.QueueManager.AttachToQueue(search))
            SetInQueue(true);
          break;
        case 1:
          var lobby = new DefaultLobbyPage(mmp);
          var status = await RiotServices.GameInvitationService.CreateArrangedTeamLobby(mmp.QueueIds[0]);
          lobby.GotLobbyStatus(status);
          Client.QueueManager.ShowPage(lobby);
          break;
      }
    }

    private async void PlayRanked(int button) {
      Close?.Invoke(this, new EventArgs());
      var mmp = new MatchMakerParams { QueueIds = new[] { selected.ID } };
      switch (button) {
        case 0:
          var search = await RiotServices.MatchmakerService.AttachToQueue(mmp);
          if (Client.QueueManager.AttachToQueue(search))
            SetInQueue(true);
          break;
        case 1:
          //TODO Ranked Duo Lobby
          var lobby = new DefaultLobbyPage(mmp);
          var status = await RiotServices.GameInvitationService.CreateArrangedTeamLobby(mmp.QueueIds[0]);
          lobby.GotLobbyStatus(status);
          Client.QueueManager.ShowPage(lobby);
          break;
      }
    }

    private void PlayRankedTeams(int button) {
      //TODO Ranked team lobby
      switch (button) {
        case 0:
          TeamCombo.ItemsSource = Client.RankedTeamInfo.PlayerTeams;
          TeamCombo.SelectedIndex = 0;
          PopupPanel.BeginStoryboard(App.FadeIn);
          break;
      }
    }

    private void PlayBots(int button) {
      Close?.Invoke(this, new EventArgs());
      //TODO Bots
      switch (button) {
        case 0:

          break;
        case 1:

          break;
      }
    }
    #endregion

    #region UI Events
    private void QueueButton1_Click(object sender, RoutedEventArgs e) {
      selected.EnterQueue(0);
    }

    private void QueueButton2_Click(object sender, RoutedEventArgs e) {
      selected.EnterQueue(1);
    }

    private async void RankedCreate_Click(object sender, RoutedEventArgs e) {
      var team = (TeamInfo) TeamCombo.SelectedItem;
      var mmp = new MatchMakerParams { QueueIds = new[] { selected.ID }, TeamId = team.TeamId };
      var lobby = new DefaultLobbyPage(mmp);
      var status = await RiotServices.GameInvitationService.CreateArrangedRankedTeamLobby(mmp.QueueIds[0], team.Name);
      lobby.GotLobbyStatus(status);
      Client.QueueManager.ShowPage(lobby);
    }

    private void CreateCustomButton_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      Client.QueueManager.ShowPage(new CustomCreatePage());
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) {
      RiotServices.MatchmakerService.PurgeFromQueues();
      SetInQueue(false);
    }

    private void JoinCustomButton_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void PopupClose_Click(object sender, RoutedEventArgs e) {
      PopupPanel.BeginStoryboard(App.FadeOut);
    }
    #endregion

    //private class QueueCategory : IEnumerable<Queue> {
    //  public string Name { get; }
    //  public string Description { get; }
    //  public List<Queue> Queues { get; } = new List<Queue>();

    //  public QueueCategory(string name, string description) {
    //    Name = name;
    //    Description = description;
    //  }

    //  public void Add(Queue q) => Queues.Add(q);

    //  public IEnumerator<Queue> GetEnumerator() => Queues.GetEnumerator();
    //  IEnumerator IEnumerable.GetEnumerator() => Queues.GetEnumerator();
    //}

    private class Queue {
      public string Name { get; }
      public int ID { get; }
      public string Action1 { get; }
      public string Action2 { get; }
      public Action<int> EnterQueue;

      public Queue(string name, int id, string act1, string act2, Action<int> enter) {
        Name = name;
        ID = id;
        Action1 = act1;
        Action2 = act2;
        EnterQueue = enter;
      }
    }
  }
}
