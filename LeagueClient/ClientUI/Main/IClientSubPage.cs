using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Queueing;

namespace LeagueClient.ClientUI.Main {
  public interface IClientSubPage {
    event EventHandler Close;

    bool CanPlay();
    void ForceClose();
    IQueuer HandleClose();
    System.Windows.Controls.Page GetPage();
  }
}
