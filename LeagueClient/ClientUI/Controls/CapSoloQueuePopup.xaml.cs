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
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueuePopup.xaml
  /// </summary>
  public partial class CapSoloQueuePopup : UserControl, IQueuePopup {
    public event EventHandler Accepted;
    public event EventHandler Cancelled;
    private JSONObject payload;
    private CapMePlayer player;

    public CapSoloQueuePopup(JSONObject payload, CapMePlayer player) {
      InitializeComponent();
      this.payload = payload;
      this.player = player;

      TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds(payload["candidateAutoQuitTimeout"])));
      var time = new Timer(payload["candidateAutoQuitTimeout"] * 1000);
      time.Start();
      time.Elapsed += Time_Elapsed;
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      (sender as IDisposable).Dispose();
      if (Cancelled != null) Cancelled(this, new EventArgs());
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      if (Accepted != null) Accepted(this, new EventArgs());
      Client.QueueManager.JoinCapLobby((string) payload["groupId"], (int) payload["slotId"], player);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      if (Cancelled != null) Cancelled(this, new EventArgs());
      RiotCalls.CapService.Quit();
    }

    public Control GetControl() {
      return this;
    }
  }
}
