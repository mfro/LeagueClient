using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;

namespace LeagueClient.UI.Client {
  public interface IClientSubPage : IDisposable {
    event EventHandler Close;

    /// <summary>
    /// The page object to render
    /// </summary>
    System.Windows.Controls.Page Page { get; }
  }
}
