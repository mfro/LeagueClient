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
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for BingeQueuer.xaml
  /// </summary>
  public partial class BingeQueuer : UserControl, IQueuer {
    public event QueuePoppedEventHandler Popped;

    private Timer timer;
    private DateTime start;
    private TimeSpan timeout;

    public BingeQueuer(int timeout) {
      InitializeComponent();

      this.timeout = TimeSpan.FromMilliseconds(timeout);
      ElapsedText.Text = this.timeout.ToString("m\\:ss");
      start = DateTime.Now;
      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed; ;
      timer.Start();
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var time = timeout.Subtract(DateTime.Now.Subtract(start));
      if (time.TotalMilliseconds < 0) Popped?.Invoke(this, new QueuePoppedEventArgs(null));
      Dispatcher.Invoke(() => ElapsedText.Text = time.ToString("m\\:ss"));
    }

    public Control GetControl() {
      return this;
    }

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
