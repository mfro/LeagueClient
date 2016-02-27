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
using MFroehlich.League.DataDragon;
using RiotClient.Riot.Platform;

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for RuneListItem.xaml
  /// </summary>
  public partial class RuneListItem : UserControl {
    public SummonerRune Rune { get; private set; }

    public RuneListItem() {
      InitializeComponent();
    }

    public RuneListItem(SummonerRune item, int count) : this() {
      Rune = item;

      var rune = DataDragon.RuneData.Value.data[item.RuneId.ToString()];
      if (rune.rune.type.Equals("black")) RuneIcon.Width = RuneIcon.Height;
      RuneIcon.Source = DataDragon.GetRuneImage(rune).Load();
      RuneName.Text = rune.name;
      RuneStats.Text = rune.description;
      RuneCount.Text = "x" + count;
    }
  }
}
