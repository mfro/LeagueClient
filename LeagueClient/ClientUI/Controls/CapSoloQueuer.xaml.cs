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
    public event QueuePoppedEventHandler Popped;

    private Timer timer;
    private DateTime start;
    private Logic.Cap.CapPlayer player;

    public CapSoloQueuer(Logic.Cap.CapPlayer player) {
      this.player = player;
      Client.MessageReceived += Client_MessageReceived;
      InitializeComponent();
      QueryPane.DataContext = player;
      PositionText.Text = player.Position.Value;
      RoleText.Text = player.Role.Value;
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
            timer.Dispose();
            e.Handled = true;
            Client.MessageReceived -= Client_MessageReceived;
            Dispatcher.Invoke(
              () => Popped?.Invoke(this, new QueuePoppedEventArgs(new CapSoloQueuePopup(JSON.ParseObject(response.payload), this.player))));
            break;
        }
      }
    }

    public Control GetControl() {
      return this;
    }

    private void This_MouseEnter(object sender, MouseEventArgs e) {
      QueryPane.BeginStoryboard(App.FadeIn);
      QueuePane.BeginStoryboard(App.FadeOut);
    }

    private void This_MouseLeave(object sender, MouseEventArgs e) {
      QueuePane.BeginStoryboard(App.FadeIn);
      QueryPane.BeginStoryboard(App.FadeOut);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) {
      timer.Dispose();
      Client.MessageReceived -= Client_MessageReceived;
      Logic.Riot.RiotCalls.CapService.Quit();
      Popped?.Invoke(this, new QueuePoppedEventArgs(null));
    }
  }
}
