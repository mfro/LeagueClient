using RiotClient;
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
    public ChatStatus WhileIdle { get; set; }
    public ChatStatus WhileQueueing { get; set; }

    private DateTime start;
    private Timer timer;
    private Label label;

    public QueueController(Label label, ChatStatus queue, ChatStatus idle) {
      timer = new Timer(200);
      timer.Elapsed += Timer_Elapsed;
      this.label = label;
      WhileIdle = idle;
      WhileQueueing = queue;
    }

    private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
      var elapsed = DateTime.Now.Subtract(start);
      Application.Current.Dispatcher.Invoke(() => label.Content = elapsed.ToString("m\\:ss"));
    }

    public void Start() {
      Session.Current.ChatManager.Status = WhileQueueing;
      start = DateTime.Now;
      timer.Start();
      Timer_Elapsed(timer, null);
    }

    public void Cancel() {
      Session.Current.ChatManager.Status = WhileIdle;
      timer.Stop();
    }

    public void Dispose() {
      timer.Dispose();
    }
  }
}
