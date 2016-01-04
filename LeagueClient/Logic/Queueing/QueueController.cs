using LeagueClient.Logic.Riot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace LeagueClient.Logic.Queueing {
  public sealed class QueueController : IDisposable {
    public bool InQueue => timer.Enabled;
    public ChatStatus Idle { get; set; }
    public ChatStatus Queue { get; set; }

    private DateTime start;
    private Timer timer;
    private Label label;

    public QueueController(Label label, ChatStatus queue, ChatStatus idle) {
      timer = new Timer(1000);
      timer.Elapsed += Timer_Elapsed;
      this.label = label;
      this.Idle = idle;
      this.Queue = queue;
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Application.Current.Dispatcher.Invoke(() => label.Content = elapsed.ToString("m\\:ss"));
    }

    public void Start() {
      Client.Session.ChatManager.Status = Queue;
      start = DateTime.Now;
      timer.Start();
      Timer_Elapsed(timer, null);
    }

    public void Cancel() {
      Client.Session.ChatManager.Status = Idle;
      timer.Stop();
    }

    public void Dispose() {
      timer.Dispose();
    }
  }
}
