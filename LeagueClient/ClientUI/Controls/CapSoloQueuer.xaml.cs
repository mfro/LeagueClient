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
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueueHandler.xaml
  /// </summary>
  public partial class CapSoloQueuer : UserControl, IQueuer {
    public event EventHandler Popped;

    private Timer timer;
    private DateTime start;
    private CapMePlayer player;

    public CapSoloQueuer(CapMePlayer player) {
      this.player = player;
      Client.MessageReceived += Client_MessageReceived;
      InitializeComponent();
      ElapsedText.Text = "0:00";
      start = DateTime.Now;
      timer = new Timer(1000);
      timer.Elapsed += Time_Elapsed;
      timer.Start();
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Dispatcher.Invoke(() => ElapsedText.Text = "" + elapsed.ToString("m\\:ss"));
    }

    private void Client_MessageReceived(object sender, MessageHandlerArgs e) {
      if (e.Handled) return;
      var response = e.InnerEvent.Body as LcdsServiceProxyResponse;
      if (response != null) {
        switch (response.methodName) {
          case "acceptedByGroupV2":
            if (Popped != null) Popped(this, new EventArgs());
            Client.MessageReceived -= Client_MessageReceived;
            timer.Dispose();
            Dispatcher.Invoke(() => Client.QueueManager.ShowQueuePopPopup(new CapSoloQueuePopup(JSON.ParseObject(response.payload), this.player)));
            break;
        }
      }
    }

    public Control GetControl() {
      return this;
    }
  }
}
