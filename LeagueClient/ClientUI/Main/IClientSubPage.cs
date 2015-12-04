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

    /// <summary>
    /// The page object to render
    /// </summary>
    System.Windows.Controls.Page Page { get; }

    /// <summary>
    /// Indicates to the page that it is closed permanently and should disconnect
    /// from any chats or lobbies
    /// </summary>
    void ForceClose();

    /// <summary>
    /// Handles a message recieved from the RTMP connection
    /// </summary>
    /// <param name="args">The message recieved</param>
    /// <returns>True if the message was handled, false to continue propagation</returns>
    bool HandleMessage(MessageReceivedEventArgs args);
  }
}
