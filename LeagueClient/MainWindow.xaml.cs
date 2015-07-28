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
      if (LeagueData.IsCurrent) {
        PatchComplete();
      } else
        Content = new PatcherPage();
    }

    public void LoginComplete() {
      var page = new ClientUI.Main.ClientPage();
      Client.QueueManager = page;
      Content = page;
    }

    public void PatchComplete() {
      Content = new LoginPage();
    }
  }
}
