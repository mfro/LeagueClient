using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.Logic {
  public interface IQueuer {
    event EventHandler Popped;

    System.Windows.Controls.Control GetControl();
  }
}
