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
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using MFroehlich.Parsing.JSON;
using RtmpSharp.Messaging;
using System.Threading;
using System.Timers;
using MFroehlich.League.Assets;
using LeagueClient.UI.Client.Alerts;
using LeagueClient.UI.Client.Lobbies;
using LeagueClient.UI.Client.Custom;
using RiotClient.Lobbies;
using RiotClient;
using RiotClient.Riot.Platform;

namespace LeagueClient.UI.Client {
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

    private GameQueue selected;
    private QueueController queueTimer;
    private List<GameQueue> queues;

    private Queue queue;

    public PlaySelectPage() {
      InitializeComponent();

      #region Queues
      queues = new List<GameQueue> {
      //queues[ClassicQueues] = new List<Queue> {
        new GameQueue("Teambuilder Draft", 400, null, "Create Group", PlayTBD),
        //new GameQueue("Teambuilder", 61, "Enter Soloqueue", "Create Lobby", PlayTeambuilder),
        new GameQueue("Blind Pick 5v5", 2, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new GameQueue("Draft Pick 5v5", 14, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new GameQueue("Blind Pick 3v3", 8, "Enter Soloqueue", "Create Lobby", PlayStandard),
      //};

      //queues[SpecialQueues] = new List<Queue> {
        new GameQueue("Blind Pick Dominion", 16, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new GameQueue("Draft Pick Dominion", 17, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new GameQueue("ARAM", 65, "Enter Soloqueue", "Create Lobby", PlayStandard),
        new GameQueue("King Poro", 300, "Enter Soloqueue", "Create Lobby", PlayStandard),
      //};

      //queues[RankedQueues] = new List<Queue> {
        new GameQueue("Ranked Solo / Duo Queue", 410, null, "Create Group", PlayTBD),
        new GameQueue("Ranked Solo / Duo Queue", 4, "Enter Soloqueue", "Invite Duo Partner", PlayRanked),
        new GameQueue("Ranked Teams 5v5", 42, null, "Create Lobby", PlayRankedTeams),
        new GameQueue("Ranked Teams 3v3", 41, null, "Create Lobby", PlayRankedTeams),
      };

      #endregion

      if (!Session.Installed.GameVersion.Equals(Session.Latest.GameVersion))
        PatchGrid.Visibility = Visibility.Visible;

      queueTimer = new QueueController(QueueLabel, ChatStatus.inQueue, ChatStatus.outOfGame);
      SummonersRift.Tag = GameMap.SummonersRift;
    }

    private void Queue_QueuePopped(object sender, GameDTO e) {
      Dispatcher.Invoke(() => {
        var popup = new DefaultQueuePopup(queue);
        LoLClient.QueueManager.ShowQueuePopup(popup);
      });
    }

    private void Queue_QueueCancelled(object sender, EventArgs e) {
      SetInQueue(false);
    }

    private void SetInQueue(bool inQueue) {
      if (inQueue) {
        queueTimer.Start();
        Dispatcher.Invoke(() => {
          CreateCustomButton.Visibility = JoinCustomButton.Visibility = QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Collapsed;
          QueueLabel.Visibility = CancelButton.Visibility = Visibility.Visible;
          Queues.IsEnabled = !inQueue;
        });
      } else {
        queueTimer.Cancel();
        Dispatcher.Invoke(() => {
          CreateCustomButton.Visibility = JoinCustomButton.Visibility = QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Visible;
          QueueLabel.Visibility = CancelButton.Visibility = Visibility.Collapsed;
          Queues.IsEnabled = !inQueue;
        });
      }
    }

    private void QueueList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      var src = sender as ListBox;
      if (src.SelectedIndex < 0) return;
      var queue = (GameQueue) ((ListBox) sender).SelectedItem;

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
                 where Session.Current.AvailableQueues.ContainsKey(item.ID)
                 select item;

#if DEBUG
      foreach (var queue in Session.Current.AvailableQueues) {
        if (!queues.Any(q => q.ID == queue.Key)) {

        }
      }
#endif

      //((Grid) category.Key.Parent).Visibility = list.Count() > 0 ? Visibility.Visible : Visibility.Collapsed;
      Queues.ItemsSource = list;
    }

    public void Dispose() {
      //update.Abort();
      queueTimer.Dispose();
    }

    #region Plays

    private void PlayTBD(int button) {
      Close?.Invoke(this, new EventArgs());

      var lobby = TBDLobby.CreateLobby(selected.ID);
      LoLClient.QueueManager.JoinLobby(lobby);
    }

    //private void PlayTeambuilder(int button) {
    //  Close?.Invoke(this, new EventArgs());
    //  switch (button) {
    //    case 0:
    //      Client.QueueManager.ShowPage(new CapSoloPage());
    //      break;
    //    case 1:
    //      Client.QueueManager.ShowPage(new CapLobbyPage(true));
    //      RiotServices.CapService.CreateGroup();
    //      break;
    //  }
    //}

    private async void PlayStandard(int button) {
      Close?.Invoke(this, new EventArgs());
      switch (button) {
        case 0:
          try {
            var mmp = new MatchMakerParams { QueueIds = new[] { selected.ID } };
            queue = await Queue.Create(mmp);
            queue.QueuePopped += Queue_QueuePopped;
            queue.QueueCancelled += Queue_QueueCancelled;
            SetInQueue(true);
          } catch {

          }
          break;
        case 1:
          var lobby = QueueLobby.CreateLobby(selected.ID);
          LoLClient.QueueManager.JoinLobby(lobby);
          break;
      }
    }

    private async void PlayRanked(int button) {
      Close?.Invoke(this, new EventArgs());
      switch (button) {
        case 0:
          try {
            var mmp = new MatchMakerParams { QueueIds = new[] { selected.ID } };
            queue = await Queue.Create(mmp);
            queue.QueuePopped += Queue_QueuePopped;
            queue.QueueCancelled += Queue_QueueCancelled;
            SetInQueue(true);
          } catch {

          }
          break;
        case 1:
          //TODO Ranked Duo Lobby
          var lobby = QueueLobby.CreateLobby(selected.ID);
          LoLClient.QueueManager.JoinLobby(lobby);
          break;
      }
    }

    private void PlayRankedTeams(int button) {
      //TODO Ranked team lobby
      switch (button) {
        case 0:
          TeamCombo.ItemsSource = Session.Current.RankedTeamInfo.PlayerTeams;
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

    private void RankedCreate_Click(object sender, RoutedEventArgs e) {
      //var team = (TeamInfo) TeamCombo.SelectedItem;
      //var mmp = new MatchMakerParams { QueueIds = new[] { selected.ID }, TeamId = team.TeamId };
      //var lobby = new DefaultLobbyPage(mmp);
      //var status = await RiotServices.GameInvitationService.CreateArrangedRankedTeamLobby(mmp.QueueIds[0], team.Name);
      //lobby.GotLobbyStatus(status);
      //Client.Session.QueueManager.ShowPage(lobby);
    }

    private void CreateCustomButton_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      LoLClient.QueueManager.ShowPage(new CustomCreatePage());
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) {
      queue.Cancel();
      SetInQueue(false);
    }

    private void JoinCustomButton_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void PopupClose_Click(object sender, RoutedEventArgs e) {
      PopupPanel.BeginStoryboard(App.FadeOut);
    }

    #endregion

    private class GameQueue {
      public string Name { get; }
      public int ID { get; }
      public string Action1 { get; }
      public string Action2 { get; }
      public Action<int> EnterQueue;

      public GameQueue(string name, int id, string act1, string act2, Action<int> enter) {
        Name = name;
        ID = id;
        Action1 = act1;
        Action2 = act2;
        EnterQueue = enter;
      }
    }
  }
}
