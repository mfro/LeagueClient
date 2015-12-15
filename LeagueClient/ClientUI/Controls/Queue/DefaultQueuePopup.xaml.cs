using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for DefaultQueuePopup.xaml
  /// </summary>
  public partial class DefaultQueuePopup : UserControl, IQueuePopup {
    public event EventHandler<QueueEventArgs> Close;

    public GameDTO GameData { get; private set; }

    public DefaultQueuePopup() {
      InitializeComponent();
    }

    public DefaultQueuePopup(GameDTO game) : this() {
      GotGameData(game);
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      (sender as IDisposable).Dispose();
    }

    private void GotGameData(GameDTO game) {
      if (GameData == null) {
        TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds(game.JoinTimerDuration)));
        var time = new Timer(game.JoinTimerDuration);
        time.Start();
        time.Elapsed += Time_Elapsed;
      }
      GameData = game;
      if (game.GameState.Equals("JOINING_CHAMP_SELECT")) {
        ParticipantPanel.Children.Clear();
        Client.Log("Players:");
        foreach (var participant in game.TeamOne.Concat(game.TeamTwo)) {
          var player = participant as PlayerParticipant;
          if (player != null) {
            //Client.SummonerCache.GetData(player.AccountId, item => {
            //  Client.Log($"  {player.SummonerName}, {player.SummonerId}");
            //  foreach (var league in item.Leagues.SummonerLeagues) {
            //    Client.Log($"    {QueueType.Values[league.Queue].Value}: {RankedTier.Values[league.Tier].Value} {league.Rank} ({league.DivisionName})");
            //  }
            //});
          }
        }
        foreach (char player in game.StatusOfParticipants) {
          var border = new Border { Width = 16, Height = 20 };
          border.BorderBrush = App.ForeBrush;
          border.BorderThickness = new Thickness(1);
          border.Margin = new Thickness(2, 0, 2, 0);
          switch (player) {
            case '0':
              border.Background = App.Back1Brush;
              break;
            case '1':
              border.Background = App.ChatBrush;
              break;
            case '2':
              border.Background = App.AwayBrush;
              break;
            default: break;
          }
          ParticipantPanel.Children.Add(border);
        }
      } else if (game.GameState.Contains("CHAMP_SELECT")) {
        Close?.Invoke(this, new QueueEventArgs(QueuePopOutcome.Accepted));
        Client.QueueManager.BeginChampionSelect(game);
      } else if (game.GameState.Equals("TERMINATED")) {
        Close?.Invoke(this, new QueueEventArgs(QueuePopOutcome.Cancelled));
      } else { }
    }

    public bool HandleMessage(MessageReceivedEventArgs e) {
      var game = e.Body as GameDTO;
      if (game != null) {
        Dispatcher.MyInvoke(GotGameData, game);
        return true;
      }
      return false;
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.AcceptPoppedGame(true);
      AcceptButt.IsEnabled = DeclineButt.IsEnabled = false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.AcceptPoppedGame(false);
      AcceptButt.IsEnabled = DeclineButt.IsEnabled = false;
    }

    public Control Control => this;
  }
}
