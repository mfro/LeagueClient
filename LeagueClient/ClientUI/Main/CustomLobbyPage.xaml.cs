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
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Queueing;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;

namespace LeagueClient.ClientUI.Main {
  /// <summary>
  /// Interaction logic for CustomLobbyPage.xaml
  /// </summary>
  public partial class CustomLobbyPage : Page, IClientSubPage {
    public CustomLobbyPage(GameDTO game) {
      InitializeComponent();
      Client.ChatManager.UpdateStatus(ChatStatus.hostingPracticeGame);
    }

    public event EventHandler Close;

    public bool CanPlay() => false;
    public Page GetPage() => this;

    public void ForceClose() {
      RiotCalls.GameService.QuitGame();
    }

    public IQueuer HandleClose() {
      return new ReturnToLobbyQueuer(this);
    }

    private void Quit_Click(object sender, RoutedEventArgs e) {
      Close?.Invoke(this, new EventArgs());
      ForceClose();
    }
  }
}
