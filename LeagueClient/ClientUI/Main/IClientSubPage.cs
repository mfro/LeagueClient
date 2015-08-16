using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  public interface IClientSubPage {
    event EventHandler Close;

    bool CanPlay { get; }
    System.Windows.Controls.Page Page { get; }

    void ForceClose();
    IQueuer HandleClose();

    bool HandleMessage(MessageReceivedEventArgs args);
  }
}
