using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
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

namespace LeagueClient.UI.Client.Profile {
  /// <summary>
  /// Interaction logic for ChampMastery.xaml
  /// </summary>
  public partial class ChampMastery : UserControl {
    private static Tuple<int, int>[] Margins = new[] {
      new Tuple<int, int>(-16, -24),
      new Tuple<int, int>(-14, -18),
      new Tuple<int, int>(-14, -12),
      new Tuple<int, int>(-11, -9),
      new Tuple<int, int>(-11, -9),
      new Tuple<int, int>(-11, -9),
      new Tuple<int, int>(-10, -6),
    };
    public ChampMastery(ChampionDto champ, int rank) {
      InitializeComponent();

      ChampImage.Source = DataDragon.GetChampIconImage(champ).Load();
      RankImage.Source = new BitmapImage(new Uri($"pack://application:,,,/RiotAPI;component/Resources/ChampMastery{rank + 1}.png"));
    }
  }
}
