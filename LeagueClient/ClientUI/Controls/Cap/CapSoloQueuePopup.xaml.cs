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
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueuePopup.xaml
  /// </summary>
  public partial class CapSoloQueuePopup : UserControl, IQueuePopup {
    private JSONObject payload;
    private Logic.Cap.CapPlayer player;
    private Timer time;

    public CapSoloQueuePopup(JSONObject payload, Logic.Cap.CapPlayer player) {
      InitializeComponent();
      this.payload = payload;
      this.player = player;

      TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds(payload["candidateAutoQuitTimeout"])));
      time = new Timer(payload["candidateAutoQuitTimeout"] * 1000);
      time.Start();
      time.Elapsed += Time_Elapsed;
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      (sender as IDisposable).Dispose();
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      RiotCalls.CapService.IndicateGroupAcceptanceAsCandidate((int) payload["slotId"], true, (string) payload["groupId"]);
      time.Dispose();
      Client.QueueManager.ShowPage(new CapLobbyPage(player));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      time.Dispose();
      RiotCalls.CapService.Quit();
    }

    public Control GetControl() {
      return this;
    }

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
