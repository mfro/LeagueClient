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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for DefaultQueuer.xaml
  /// </summary>
  public partial class DefaultQueuer : UserControl, IQueuer {
    public event QueuePoppedEventHandler Popped;

    private Timer timer;
    private DateTime start;

    public DefaultQueuer(GameQueueConfig queue, string bots) {
      InitializeComponent();
      Client.MessageReceived += Client_MessageReceived;
      QueueName.Text = QueueType.Values[queue.Type].Value;
      ElapsedText.Text = "In queue for 0:00";

      start = DateTime.Now;
      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed;
      timer.Start();
    }

    private void Client_MessageReceived(object sender, MessageHandlerArgs e) {

    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Dispatcher.Invoke(() => ElapsedText.Text = "In queue for " + elapsed.ToString("m\\:ss"));
    }

    private void Cancel_Click(object src, EventArgs args) {
      Popped?.Invoke(this, new QueuePoppedEventArgs(null));
    }

    public Control GetControl() {
      return this;
    }
  }
}
