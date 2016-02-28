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
using RtmpSharp.Messaging;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using RiotClient.Riot.Platform;
using RiotClient.Lobbies;

namespace LeagueClient.UI.Client.Alerts {
  /// <summary>
  /// Interaction logic for DefaultQueuePopup.xaml
  /// </summary>
  public partial class DefaultQueuePopup : UserControl, IQueuePopup {
    public event EventHandler<QueueEventArgsa> Close;

    public GameDTO GameData { get; private set; }
    public Queue Queue { get; }

    public DefaultQueuePopup(Queue queue) {
      InitializeComponent();

      Queue = queue;

      queue.QueuePopped += Queue_QueuePopped;
      queue.QueuePopUpdated += Queue_QueuePopUpdated;
      queue.EnteredChampSelect += Queue_EnteredChampSelect;
      queue.QueueCancelled += Queue_QueueCancelled;
    }

    private void Queue_QueuePopped(object sender, GameDTO e) {
      TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds(e.JoinTimerDuration)));
    }

    private void Queue_QueuePopUpdated(object sender, Queue.QueuePopPlayerState[] e) {
      Dispatcher.Invoke(() => {
        ParticipantPanel.Children.Clear();
        foreach (var state in e) {
          var border = new Border { Width = 16, Height = 20 };
          border.BorderBrush = App.ForeBrush;
          border.BorderThickness = new Thickness(1);
          border.Margin = new Thickness(2, 0, 2, 0);
          switch (state) {
            case Queue.QueuePopPlayerState.None:
              border.Background = App.Back1Brush;
              break;
            case Queue.QueuePopPlayerState.Accepted:
              border.Background = App.ChatBrush;
              break;
            case Queue.QueuePopPlayerState.Declined:
              border.Background = App.AwayBrush;
              break;
            default: break;
          }
          ParticipantPanel.Children.Add(border);
        }
      });
    }

    private void Queue_EnteredChampSelect(object sender, GameLobby e) {
      LoLClient.MainWindow.BeginChampionSelect(e);
    }

    private void Queue_QueueCancelled(object sender, object e) {
      Close?.Invoke(this, new QueueEventArgsa(QueuePopOutcome.Cancelled));
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      Queue.React(true);
      AcceptButt.IsEnabled = DeclineButt.IsEnabled = false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      Queue.React(false);
      AcceptButt.IsEnabled = DeclineButt.IsEnabled = false;
    }

    public Control Control => this;
  }
}
