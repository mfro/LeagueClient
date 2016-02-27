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
using LeagueClient.Logic;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using RiotClient;

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for ProfileIconSelector.xaml
  /// </summary>
  public partial class ProfileIconSelector : UserControl {
    public event EventHandler<RiotClient.Riot.Platform.Icon> IconSelected;

    public ProfileIconSelector() {
      InitializeComponent();

      if (Session.Current.Connected)
        LoadIcons();
    }

    public async void LoadIcons() {
      var icons = await Session.Current.Account.GetSummonerIconInventory();
      var data = new List<object>();
      foreach (var icon in icons.SummonerIcons.OrderByDescending(i => i.PurchaseDate)) {
        ProfileIconDto info;
        if (DataDragon.IconData.Value.data.TryGetValue(icon.IconId.ToString(), out info)) {
          var image = DataDragon.GetProfileIconImage(info);
          if (image.Exists)
            data.Add(new IconInfo { Image = image.Load(), Icon = icon });
        }
      }
      Dispatcher.Invoke(() => IconsGrid.ItemsSource = data);
    }

    private void Icon_Click(object sender, MouseButtonEventArgs e) {
      var data = ((sender as Border).DataContext as IconInfo).Icon;
      IconSelected?.Invoke(this, data);
    }

    private class IconInfo {
      public BitmapImage Image { get; set; }
      public RiotClient.Riot.Platform.Icon Icon { get; set; }
    }
  }
}
