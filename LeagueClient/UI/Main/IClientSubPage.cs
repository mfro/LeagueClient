using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;

namespace LeagueClient.UI.Main {
  public interface IClientSubPage : IDisposable {
    event EventHandler Close;

    /// <summary>
    /// The page object to render
    /// </summary>
    System.Windows.Controls.Page Page { get; }

    /// <summary>
    /// Handles a message recieved from the RTMP connection
    /// </summary>
    /// <param name="args">The message recieved</param>
    /// <returns>True if the message was handled, false to continue propagation</returns>
    bool HandleMessage(MessageReceivedEventArgs args);
  }
}
