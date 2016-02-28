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

namespace LeagueClient.UI.Client.Alerts {
  /// <summary>
  /// Interaction logic for BingeQueuer.xaml
  /// </summary>
  public sealed partial class BingeQueuer : UserControl, IQueueInfo, IDisposable {
    public event EventHandler Popped;

    private Timer timer;
    private DateTime start;
    private TimeSpan timeout;

    public BingeQueuer() {
      InitializeComponent();
    }

    public BingeQueuer(int timeout, string name = null) : this() {
      this.timeout = TimeSpan.FromMilliseconds(timeout);
      ElapsedText.Content = this.timeout.ToString("m\\:ss");

      if (name != null) {
        Body.Text = name + " has dodged too many games recently";
      }

      start = DateTime.Now;
      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed; ;
      timer.Start();
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var time = timeout.Subtract(DateTime.Now.Subtract(start));
      if (time.TotalMilliseconds < 0) Popped?.Invoke(this, new EventArgs());
      Dispatcher.Invoke(() => ElapsedText.Content = time.ToString("m\\:ss"));
    }

    public Control Control => this;
    public void Dispose() => timer.Dispose();
  }
}
