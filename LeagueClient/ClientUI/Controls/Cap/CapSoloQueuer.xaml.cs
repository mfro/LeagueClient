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
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderSoloQueueHandler.xaml
  /// </summary>
  public sealed partial class CapSoloQueuer : UserControl, IQueuer, IDisposable {
    public event EventHandler<QueuePoppedEventArgs> Popped;

    private Timer timer;
    private DateTime start;
    private Logic.Cap.CapPlayer player;

    public CapSoloQueuer() {
      InitializeComponent();
    }

    public CapSoloQueuer(Logic.Cap.CapPlayer player) : this() {
      this.player = player;
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

    public bool HandleMessage(MessageReceivedEventArgs e) {
      var response = e.Body as LcdsServiceProxyResponse;
      if (response != null) {
        switch (response.methodName) {
          case "acceptedByGroupV2":
            timer.Dispose();
            Dispatcher.Invoke(() => Popped?.Invoke(this, new QueuePoppedEventArgs(new CapSoloQueuePopup(JSON.ParseObject(response.payload), this.player))));
            return true;
        }
      }
      return false;
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
      Logic.Riot.RiotServices.CapService.Quit();
      Popped?.Invoke(this, new QueuePoppedEventArgs(null));
    }

    public Control Control => this;
    public void Dispose() => timer.Dispose();
  }
}
