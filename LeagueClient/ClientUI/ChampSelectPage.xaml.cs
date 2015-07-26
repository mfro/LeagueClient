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
using LeagueClient.Logic.Riot;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for ChampSelect.xaml
  /// </summary>
  public partial class ChampSelectPage : Page {
    public ChampSelectPage() {
      InitializeComponent();
      ChampsGrid.Champions = Client.AvailableChampions;
    }

    private void SkinScroller_MouseWheel(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0)
        ((ScrollViewer) sender).LineLeft();
      else
        ((ScrollViewer) sender).LineRight();
      e.Handled = true;
    }
  }
}
