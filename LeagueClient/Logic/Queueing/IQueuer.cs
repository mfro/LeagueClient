using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;

namespace LeagueClient.Logic.Queueing {
  public delegate void QueuePoppedEventHandler(object src, QueuePoppedEventArgs args);

  public interface IQueuer {
    event QueuePoppedEventHandler Popped;

    System.Windows.Controls.Control GetControl();

    bool HandleMessage(MessageReceivedEventArgs args);
  }

  public class QueuePoppedEventArgs : EventArgs {
    public IQueuePopup QueuePopup { get; private set; }

    public QueuePoppedEventArgs(IQueuePopup popup) {
      QueuePopup = popup;
    }
  }
}
