using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using LeagueClient.ClientUI.Main;
using LeagueClient.Logic;
using LeagueClient.Logic.Queueing;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ReturnToLobbyQueuer.xaml
  /// </summary>
  public partial class ReturnToLobbyQueuer : UserControl, IQueuer {
    public event EventHandler<QueuePoppedEventArgs> Popped;

    private IClientSubPage page;

    public ReturnToLobbyQueuer(IClientSubPage lobby) {
      InitializeComponent();
      page = lobby;
    }

    private void Home_Click(object sender, RoutedEventArgs e) {
      Popped?.Invoke(this, new QueuePoppedEventArgs(null));
      Client.QueueManager.ShowPage(page);
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      Popped?.Invoke(this, new QueuePoppedEventArgs(null));
      page.ForceClose();
    }

    public Control GetControl() {
      return this;
    }

    public bool HandleMessage(MessageReceivedEventArgs args) => false;
  }
}
