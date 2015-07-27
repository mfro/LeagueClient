using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.Logic.Queueing {
  public interface IQueuePopup {
    event EventHandler Accepted;
    event EventHandler Cancelled;

    System.Windows.Controls.Control GetControl();
  }
}
