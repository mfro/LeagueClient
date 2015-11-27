using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for InGamePage.xaml
  /// </summary>
  public partial class InGamePage : Page, IClientSubPage {
    public event EventHandler Close;

    public InGamePage() {
      InitializeComponent();
      new Thread(WaitForGameClient) { IsBackground = true }.Start();
    }

    private void WaitForGameClient() {
      Client.GameProcess.WaitForExit();

    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      var game = args.Body as GameDTO;
      if (game != null) {
        if (game.GameState.Equals("TERMINATED_IN_ERROR")) {
          Client.ChatManager.UpdateStatus(ChatStatus.outOfGame);
          Close?.Invoke(this, new EventArgs());
          return true;
        } else if (game.GameState.Equals("TERMINATED")) {

        }
      }
      return false;
    }

    public Page Page => this;
    public bool CanPlay => false;
    public bool CanClose => false;

    public void ForceClose() { }
    public IQueuer HandleClose() => null;
  }
}
