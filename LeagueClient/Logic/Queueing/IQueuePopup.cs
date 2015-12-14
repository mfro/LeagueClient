using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;

namespace LeagueClient.Logic.Queueing {
  public interface IQueuePopup {
    event EventHandler<QueueEventArgs> Close;

    System.Windows.Controls.Control Control { get; }

    bool HandleMessage(MessageReceivedEventArgs args);
  }

  public class QueueEventArgs : EventArgs {
    public QueuePopOutcome Outcome { get; }
    public QueueEventArgs(QueuePopOutcome outcome) {
      Outcome = outcome;
    }
  }

  public enum QueuePopOutcome {
    /// <summary>
    /// User is returned to queue
    /// </summary>
    Cancelled,
    /// <summary>
    /// User is sent to champ select
    /// </summary>
    Accepted,
    /// <summary>
    /// User is removed from queue
    /// </summary>
    Declined
  }
}
