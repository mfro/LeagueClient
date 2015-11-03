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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for DefaultQueuePopup.xaml
  /// </summary>
  public partial class DefaultQueuePopup : UserControl, IQueuePopup {
    public event EventHandler Close;

    public GameDTO GameData { get; private set; }

    public DefaultQueuePopup(GameDTO game) {
      InitializeComponent();

      GotGameData(game);

      TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds(game.JoinTimerDuration)));
      var time = new Timer(game.JoinTimerDuration * 1000);
      time.Start();
      time.Elapsed += Time_Elapsed;
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      (sender as IDisposable).Dispose();
    }

    private void GotGameData(GameDTO game) {
      GameData = game;
      if (game.GameState.Equals("JOINING_CHAMP_SELECT")) {
        ParticipantPanel.Children.Clear();
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
            default: break;
          }
          ParticipantPanel.Children.Add(border);
        }
      } else if (game.GameState.Equals("CHAMP_SELECT")) {
        Close?.Invoke(this, new EventArgs());
        Client.QueueManager.BeginChampionSelect(game);
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
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      RiotServices.GameService.AcceptPoppedGame(false);
    }

    public Control Control => this;
  }
}
