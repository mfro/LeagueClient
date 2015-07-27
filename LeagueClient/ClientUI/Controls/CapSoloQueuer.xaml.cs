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

    public int Elapsed { get; set; }

    private Timer timer;

    public CapSoloQueuer() {
      Client.MessageReceived += Client_MessageReceived;
      InitializeComponent();
      ElapsedText.Text = "" + (Elapsed = 0);
      timer = new Timer(1000);
      timer.Elapsed += Time_Elapsed;
      timer.Start();
    }

    private void Time_Elapsed(object sender, ElapsedEventArgs e) {
      Dispatcher.Invoke(() => ElapsedText.Text = "" + (++Elapsed));
    }

    private void Client_MessageReceived(object sender, RtmpSharp.Messaging.MessageReceivedEventArgs e) {
      var response = e.Body as LcdsServiceProxyResponse;
      if(response != null) {
        switch (response.methodName) {
          case "acceptedByGroupV2":
            if (Popped != null) Popped(this, new EventArgs());
            timer.Dispose();
            Client.QueueManager.ShowQueuePopPopup(new CapSoloQueuePopup(JSON.ParseObject(response.payload)));
            break;
        }
      }
    }

    public Control GetControl() {
      return this;
    }
  }
}
