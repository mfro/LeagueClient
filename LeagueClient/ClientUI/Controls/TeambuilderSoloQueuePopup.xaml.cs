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
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueuePopup.xaml
  /// </summary>
  public partial class TeambuilderSoloQueuePopup : UserControl, IQueuePopup {
    public event EventHandler Accepted;
    public event EventHandler Cancelled;
    private JSONObject payload;
    private Timer timer;

    public TeambuilderSoloQueuePopup(JSONObject payload) {
      InitializeComponent();
      this.payload = payload;
      timer = new Timer(50);
      timer.Elapsed += Timer_Elapsed;
      timer.Start();
      TimeoutBar.Value = 1;
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      double elapsed = DateTime.Now.Subtract(e.SignalTime).TotalSeconds;
      TimeoutBar.Value = 1 - (elapsed / (double) payload["candidateAutoQuitTimeout"]);
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      if (Accepted != null) Accepted(this, new EventArgs());
      Client.MainPage.JoinTeambuilderLobby((string) payload["groupId"], (int) payload["slotId"]);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      if (Cancelled != null) Cancelled(this, new EventArgs());
    }

    public Control GetControl() {
      return this;
    }
  }
}
