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
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for PlaySelectPage.xaml
  /// </summary>
  public partial class PlaySelectPage : Page, IClientSubPage {
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

    private Border currentUI;
    private GameMap currentMap;
    private Dictionary<GameMap, List<IPlayableQueue>> queues = new Dictionary<GameMap, List<IPlayableQueue>>();

    private Action<int> ButtonAction;
    private GameQueueConfig currentConfig;

    public PlaySelectPage() {
      InitializeComponent();

      #region Queues
      queues[GameMap.SummonersRift] = new List<IPlayableQueue> {
        new PlayablePvpQueue("Team Builder", SelectTeambuilder, 61),
        new PlayablePvpQueue("Blind Pick", SelectStandard, 2),
        new PlayablePvpQueue("Draft Pick", SelectStandard, 14),
        new PlayablePvpQueue("Ranked Solo / Duo Queue", SelectStandard, 4),
        new PlayablePvpQueue("Ranked Teams", SelectRankedTeams, 42),
        new PlayableBotsQueue("Intro", SelectBots, 31, "INTRO"),
        new PlayableBotsQueue("Beginner", SelectBots, 32, "EASY"),
        new PlayableBotsQueue("Intermediate", SelectBots, 33, "MEDIUM"),
      };

      queues[GameMap.TheTwistedTreeline] = new List<IPlayableQueue> {
        new PlayablePvpQueue("Blind Pick", SelectStandard, 8),
        new PlayablePvpQueue("Ranked Teams", SelectStandard, 41),
        new PlayableBotsQueue("Beginner", SelectBots, 52, "EASY"),
        new PlayableBotsQueue("Intermediate", SelectBots, 52, "MEDIUM"),
      };

      queues[GameMap.TheCrystalScar] = new List<IPlayableQueue> {
        new PlayablePvpQueue("Blind Pick", SelectStandard, 16),
        new PlayablePvpQueue("Draft Pick", SelectStandard, 17),
        new PlayableBotsQueue("Beginner", SelectBots, 25, "EASY"),
        new PlayableBotsQueue("Intermediate", SelectBots, 25, "MEDIUM"),
      };

      queues[GameMap.HowlingAbyss] = new List<IPlayableQueue> {
        new PlayablePvpQueue("ARAM", SelectStandard, 65),
      };
      #endregion

      SummonersRift.Tag = GameMap.SummonersRift;
      CrystalScar.Tag = GameMap.TheCrystalScar;
      TwistedTreeline.Tag = GameMap.TheTwistedTreeline;
      HowlingAbyss.Tag = GameMap.HowlingAbyss;

      foreach (var border in new[] { SummonersRift, CrystalScar, TwistedTreeline, HowlingAbyss }) {
        border.MouseEnter += Border_MouseEnter;
        border.MouseLeave += Border_MouseLeave;
        border.MouseUp += Border_MouseUp;
      }

      currentUI = SummonersRift;
      MapSelected();
    }

    private void MapSelected() {
      currentUI.Effect = new DropShadowEffect { BlurRadius = 15, Color = App.FocusColor, ShadowDepth = 0 };
      if (currentMap == currentUI.Tag) return;

      currentMap = (GameMap) currentUI.Tag;
      MapNameLabel.Content = currentMap.DisplayName;
      QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Collapsed;

      var available = queues[currentMap].Where(q => q.Type == 0 || Client.AvailableQueues.ContainsKey(q.Type));
      var pvps = available.Where(q => q is PlayablePvpQueue);
      var bots = available.Where(q => q is PlayableBotsQueue);

      PvPQueueList.ItemsSource = pvps;
      BotsQueueList.ItemsSource = bots;

      if (Client.Settings.RecentQueuesByMapId == null)
        Client.Settings.RecentQueuesByMapId = new Dictionary<string, int>();
      if (Client.Settings.RecentQueuesByMapId.ContainsKey(currentMap.Name)) {
        var id = Client.Settings.RecentQueuesByMapId[currentMap.Name];

        var pvp = pvps.FirstOrDefault(q => q.Type == id);
        if (pvp != null) PvPQueueList.SelectedItem = pvp;

        var bot = bots.FirstOrDefault(q => q.Type == id);
        if (bot != null) BotsQueueList.SelectedItem = bot;
      } else if (available.Count() == 1) {
        if (pvps.Count() > 0) PvPQueueList.SelectedIndex = 0;
        else BotsQueueList.SelectedIndex = 0;
      }
    }

    private void QueueList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
      var src = sender as ListBox;
      if (src.SelectedIndex < 0) return;
      if (sender == PvPQueueList) BotsQueueList.SelectedIndex = -1;
      else PvPQueueList.SelectedIndex = -1;
      var queue = (IPlayableQueue) src.SelectedItem;
      queue.Invoke();
      if (queue.Type > 0) currentConfig = Client.AvailableQueues[queue.Type];
      else currentConfig = null;
    }

    #region Selections
    private void SelectTeambuilder(int type) {
      QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Visible;
      QueueButton1.Content = "Enter Soloqueue";
      QueueButton2.Content = "Create Lobby";
      ButtonAction = PlayTeambuilder;
    }

    private void SelectStandard(int type) {
      QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Visible;
      QueueButton1.Content = "Enter Soloqueue";
      QueueButton2.Content = "Create Lobby";
      ButtonAction = PlayStandard;
    }

    private void SelectRanked(int type) {
      QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Visible;
      QueueButton1.Content = "Enter Soloqueue";
      QueueButton2.Content = "Invite Duo Partner";
      ButtonAction = PlayRanked;
    }

    private void SelectRankedTeams(int type) {
      QueueButton1.Visibility = Visibility.Visible;
      QueueButton2.Visibility = Visibility.Collapsed;
      QueueButton1.Content = "Create Lobby";
      ButtonAction = PlayRankedTeams;
    }

    private void SelectBots(int type, string a) {
      QueueButton1.Visibility = QueueButton2.Visibility = Visibility.Visible;
      QueueButton1.Content = "Enter Soloqueue";
      QueueButton2.Content = "Create Lobby";
      ButtonAction = PlayBots;
    }
    #endregion

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
      var mmp = new MatchMakerParams { QueueIds = new[] { currentConfig.Id } };
      switch (button) {
        case 0:
          var search = await RiotServices.MatchmakerService.AttachToQueue(mmp);
          if (search.PlayerJoinFailures?.Count > 0) {
            switch (search.PlayerJoinFailures[0].ReasonFailed) {
              case "QUEUE_DODGER":
                Client.QueueManager.ShowQueuer(new BingeQueuer(search.PlayerJoinFailures[0].PenaltyRemainingTime));
                break;
            }
          } else {
            Client.QueueManager.ShowQueuer(new DefaultQueuer(mmp));
          }
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
      var mmp = new MatchMakerParams { QueueIds = new[] { currentConfig.Id } };
      switch (button) {
        case 0:
          var search = await RiotServices.MatchmakerService.AttachToQueue(mmp);
          if (search.PlayerJoinFailures?.Count > 0) {
            switch (search.PlayerJoinFailures[0].ReasonFailed) {
              case "QUEUE_DODGER":
                Client.QueueManager.ShowQueuer(new BingeQueuer(search.PlayerJoinFailures[0].PenaltyRemainingTime));
                break;
            }
          } else {
            Client.QueueManager.ShowQueuer(new DefaultQueuer(mmp));
          }
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
    private void Border_MouseEnter(object sender, MouseEventArgs e) {
      var border = sender as Border;

      border.BeginAnimation(MarginProperty, MarginShrink);
      border.BeginAnimation(Border.BorderThicknessProperty, BorderExpand);
    }

    private void Border_MouseLeave(object sender, MouseEventArgs e) {
      var border = sender as Border;

      border.BeginAnimation(MarginProperty, MarginExpand);
      border.BeginAnimation(Border.BorderThicknessProperty, BorderShrink);
    }

    private void Border_MouseUp(object sender, MouseButtonEventArgs e) {
      var border = sender as Border;

      currentUI.Effect = null;
      currentUI = border;
      MapSelected();
    }

    private void QueueButton1_Click(object sender, RoutedEventArgs e) {
      Client.Settings.RecentQueuesByMapId[currentMap.Name] = currentConfig.Id;
      ButtonAction?.Invoke(0);
    }

    private void QueueButton2_Click(object sender, RoutedEventArgs e) {
      Client.Settings.RecentQueuesByMapId[currentMap.Name] = currentConfig.Id;
      ButtonAction?.Invoke(1);
    }

    private void RankedCreate_Click(object sender, RoutedEventArgs e) {
      var team = (TeamInfo) TeamCombo.SelectedItem;
      var mmp = new MatchMakerParams { QueueIds = new[] { currentConfig.Id }, TeamId = team.TeamId  };
      var lobby = new DefaultLobbyPage(mmp);
      RiotServices.GameInvitationService.CreateArrangedRankedTeamLobby(mmp.QueueIds[0], team.Name).ContinueWith(t => lobby.GotLobbyStatus(t.Result));
      Client.QueueManager.ShowPage(lobby);
    }

    private void CreateCustomButton_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      Client.QueueManager.ShowPage(new CustomCreatePage(currentMap));
    }

    private void JoinCustomButton_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
    }

    private void PopupClose_Click(object sender, RoutedEventArgs e) {
      PopupPanel.BeginStoryboard(App.FadeOut);
    }
    #endregion

    public Page Page => this;
    public bool CanPlay => false;
    public bool CanClose => true;

    public void ForceClose() { }
    public IQueuer HandleClose() => null;

    public bool HandleMessage(MessageReceivedEventArgs args) => false;

    private class PlayableBotsQueue : IPlayableQueue {
      public string Name { get; private set; }
      public int Type { get; private set; }

      public string Bots { get; private set; }

      private Action<int, string> Selected;


      public PlayableBotsQueue(string name, Action<int, string> selected, int type, string bots) {
        Name = name;
        Selected = selected;
        Type = type;
        Bots = bots;
      }

      public void Invoke() => Selected(Type, Bots);

      public override string ToString() => Name;
    }

    private class PlayablePvpQueue : IPlayableQueue {
      public string Name { get; private set; }
      public int Type { get; private set; }

      private Action<int> Selected;

      public PlayablePvpQueue(string name, Action<int> selected, int type) {
        Name = name;
        Selected = selected;
        Type = type;
      }

      public void Invoke() => Selected(Type);

      public override string ToString() => Name;
    }

    private interface IPlayableQueue {
      string Name { get; }
      int Type { get; }
      void Invoke();
    }
  }
}
