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
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for DefaultQueuePopup.xaml
  /// </summary>
  public partial class DefaultQueuePopup : UserControl, IQueuePopup {
    public event EventHandler Accepted;
    public event EventHandler Cancelled;

    public GameDTO GameData { get; private set; }

    public DefaultQueuePopup(GameDTO game) {
      InitializeComponent();
      Client.MessageReceived += Client_MessageReceived;

      GotGameData(game);

      TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds(game.JoinTimerDuration)));
      var time = new Timer(game.JoinTimerDuration * 1000);
      time.Start();
      time.Elapsed += Time_Elapsed;
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      (sender as IDisposable).Dispose();
      if (Cancelled != null) Cancelled(this, new EventArgs());
    }

    private void GotGameData(GameDTO game) {
      GameData = game;
      foreach(char player in game.StatusOfParticipants) {
        var border = new Border { Width = 16, Height = 20 };
        border.BorderBrush = App.ForeBrush;
        border.BorderThickness = new Thickness(1);
        border.Margin = new Thickness(2, 0, 2, 0);
        switch (player) {
          case '0':
            border.Background = App.ChatBrush;
            break;
          case '1':
            border.Background = App.Back1Brush;
            break;
          default: break;
        }
        ParticipantPanel.Children.Add(border);
      }
    }

    private void Client_MessageReceived(object sender, MessageHandlerArgs e) {
      if (e.Handled) return;
      var game = e.InnerEvent.Body as GameDTO;
      if(game != null) {
        e.Handled = true;
        GotGameData(game);
      }
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      if (Accepted != null) Accepted(this, new EventArgs());
      RiotCalls.GameService.AcceptPoppedGame(true);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      if (Cancelled != null) Cancelled(this, new EventArgs());
      RiotCalls.GameService.AcceptPoppedGame(false);
    }

    public Control GetControl() {
      return this;
    }
  }
}
