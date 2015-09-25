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
    /// True if the user can play or join an new game while it is open
    /// </summary>
    bool CanPlay { get; }
    /// <summary>
    /// True if the user can navigate away from the page while it is open
    /// </summary>
    bool CanClose { get; }
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
    /// If the page is closed, usually by the user, this method is called
    /// </summary>
    /// <returns>An IQueuer to show, or null to close completely</returns>
    IQueuer HandleClose();

    /// <summary>
    /// Handles a message recieved from the RTMP connection
    /// </summary>
    /// <param name="args">The message recieved</param>
    /// <returns>True if the message was handled, false to continue propagation</returns>
    bool HandleMessage(MessageReceivedEventArgs args);
  }
}
