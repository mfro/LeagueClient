using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LeagueClient.ClientUI.Main {
  public interface IClientSubPage {
    event EventHandler Close;

    bool CanPlay();
    System.Windows.Controls.Page GetPage();
  }
}
