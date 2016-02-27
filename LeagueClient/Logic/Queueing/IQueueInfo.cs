using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.Messaging;

namespace LeagueClient.Logic.Queueing {
  public interface IQueueInfo {
    event EventHandler Popped;

    System.Windows.Controls.Control Control { get; }
  }
}
