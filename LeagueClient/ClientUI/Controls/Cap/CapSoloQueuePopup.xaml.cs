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
using MFroehlich.Parsing.JSON;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueuePopup.xaml
  /// </summary>
  public partial class CapSoloQueuePopup : UserControl, IQueuePopup {
    public event EventHandler Close;

    private JSONObject payload;
    private Logic.Cap.CapPlayer player;

    public CapSoloQueuePopup() {
      InitializeComponent();
    }

    public CapSoloQueuePopup(JSONObject payload, Logic.Cap.CapPlayer player) : this() {
      this.payload = payload;
      this.player = player;

      TimeoutBar.AnimateProgress(1, 0, new Duration(TimeSpan.FromSeconds((int) payload["candidateAutoQuitTimeout"])));
      var time = new Timer((int) payload["candidateAutoQuitTimeout"]);
      time.Start();
      time.Elapsed += Time_Elapsed;
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      (sender as IDisposable).Dispose();
      Close?.Invoke(this, new EventArgs());
    }

    private void Accept_Click(object sender, RoutedEventArgs e) {
      RiotServices.CapService.IndicateGroupAcceptanceAsCandidate((int) payload["slotId"], true, (string) payload["groupId"]);
      Client.QueueManager.ShowPage(new CapLobbyPage(player));
      Close?.Invoke(this, new EventArgs());
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      RiotServices.CapService.Quit();
      Close?.Invoke(this, new EventArgs());
    }

    public Control Control => this;

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
