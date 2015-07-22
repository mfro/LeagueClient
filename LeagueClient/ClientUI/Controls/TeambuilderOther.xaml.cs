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
using LeagueClient.RiotInterface.Riot.Platform;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderPlayer.xaml
  /// </summary>
  public partial class TeambuilderOther : UserControl {
    public TeambuilderOther(Member member) {
      InitializeComponent();
      SummonerText.Text = member.SummonerName;
      //TODO Finish This
    }
  }
}
