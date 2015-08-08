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

    public GameQueueConfig Config { get; private set; }

    private Timer timer;
    private DateTime start;

    public DefaultQueuer(MatchMakerParams mmp) {
      InitializeComponent();
      Config = Client.AvailableQueues[mmp.QueueIds[0]];
      Client.MessageReceived += Client_MessageReceived;
      QueueName.Text = QueueType.Values[Config.Type].Value;
      ElapsedText.Text = "In queue for 0:00";

      start = DateTime.Now;
      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed;
      timer.Start();
    }

    private void Client_MessageReceived(object sender, MessageHandlerArgs e) {
      if (e.Handled) return;
      var game = e.InnerEvent.Body as GameDTO;
      if(game != null) {
        timer.Dispose();
        e.Handled = true;
        Client.MessageReceived -= Client_MessageReceived;
        Popped?.Invoke(this, new QueuePoppedEventArgs(new DefaultQueuePopup(game)));
      }
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Dispatcher.Invoke(() => ElapsedText.Text = "In queue for " + elapsed.ToString("m\\:ss"));
    }

    private void Cancel_Click(object src, EventArgs args) {
      timer.Dispose();
      Client.MessageReceived -= Client_MessageReceived;
      Logic.Riot.RiotCalls.MatchmakerService.CancelFromQueueIfPossible(Client.LoginPacket.AllSummonerData.Summoner.SumId);
      Popped?.Invoke(this, new QueuePoppedEventArgs(null));
    }

    public Control GetControl() {
      return this;
    }
  }
}
