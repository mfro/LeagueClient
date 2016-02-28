using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;
using System.Diagnostics;
using RiotClient;
using RiotClient.Riot.Platform;

namespace LeagueClient.UI.Client {
  /// <summary>
  /// Interaction logic for InGamePage.xaml
  /// </summary>
  public sealed partial class InGamePage : Page, IClientSubPage {
    public event EventHandler Close;
    private Process game;
    private bool champSelect;

    public InGamePage(bool champSelect = false, bool canQuit = false) {
      InitializeComponent();

      this.champSelect = champSelect;
      if (champSelect) {
        ReconnectGrid.Visibility = Visibility.Visible;
        ReconnectButton.Content = "Show Champion Select";
      } else {
        ReconnectButton.Content = "Reconnect";
        ReconnectGrid.Visibility = Visibility.Collapsed;

        var procs = Process.GetProcessesByName("League of Legends");
        if (procs.Length > 0) {
          game = procs[0];
        } else return;

        new Thread(WaitForGameClient) { IsBackground = true }.Start();
      }
    }

    private void WaitForGameClient() {
      game.WaitForExit();

      Dispatcher.Invoke(() => {
        ReconnectGrid.Visibility = Visibility.Visible;
        ReconnectButton.Content = "Reconnect";
      });
    }

    private void ReconnectButton_Click(object sender, RoutedEventArgs e) {
      if (champSelect)
        LoLClient.MainWindow.ShowChampSelect();
      else Session.Current.JoinGame();
    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      var game = args.Body as GameDTO;
      if (game != null) {
        if (game.GameState.Equals("TERMINATED_IN_ERROR")) {
          Session.Current.ChatManager.Status = ChatStatus.outOfGame;
          Close?.Invoke(this, new EventArgs());
          return true;
        } else if (game.GameState.Equals("TERMINATED")) {
          Session.Current.ChatManager.Status = ChatStatus.outOfGame;
          Close?.Invoke(this, new EventArgs());
          return true;
        }
      }
      return false;
    }

    public Page Page => this;

    public void Dispose() { }
  }
}
