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
using MFroehlich.League.Assets;
using LeagueClient.ClientUI;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using RtmpSharp.Net;
using System.Security.Cryptography;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using System.Windows.Threading;

namespace LeagueClient {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      Client.Log("Pre-Init");
      Client.PreInitialize(this);
      Client.Log(LeagueData.CurrentVersion);

      InitializeComponent();
      ((App) App.Current).LoadResources();
      if (!PatcherPage.NeedsPatch()) {
        PatchComplete();
      } else
        ContentFrame.Content = new PatcherPage();
    }

    public void LoginComplete() {
      var page = new ClientUI.Main.ClientPage();
      Client.QueueManager = page;
      ContentFrame.Content = page;
    }

    public void PatchComplete() {
      ContentFrame.Content = new LoginPage();
    }

    private void Close_Click(object sender, RoutedEventArgs e) {
      Close();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) {
      WindowState = WindowState.Minimized;
    }
  }
}
