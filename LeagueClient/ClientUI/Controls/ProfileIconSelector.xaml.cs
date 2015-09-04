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
using LeagueClient.Logic.Riot;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ProfileIconSelector.xaml
  /// </summary>
  public partial class ProfileIconSelector : UserControl {
    public event EventHandler<Logic.Riot.Platform.Icon> IconSelected;

    public ProfileIconSelector() {
      InitializeComponent();

      if (Client.Connected)
        LoadIcons();
    }

    public async void LoadIcons() {
      var icons = await RiotServices.SummonerIconService.GetSummonerIconInventory(Client.LoginPacket.AllSummonerData.Summoner.SumId);
      var data = new List<object>();
      foreach (var icon in icons.SummonerIcons.OrderByDescending(i => i.PurchaseDate))
        data.Add(new IconInfo { Image = LeagueData.GetProfileIconImage(icon.IconId), Icon = icon });
      Dispatcher.Invoke(() => IconsGrid.ItemsSource = data);
    }

    private void Icon_Click(object sender, MouseButtonEventArgs e) {
      var data = ((sender as Border).DataContext as IconInfo).Icon;
      Client.LoginPacket.AllSummonerData.Summoner.ProfileIconId = Client.Settings.ProfileIcon = data.IconId;
      IconSelected?.Invoke(this, data);
      RiotServices.SummonerService.UpdateProfileIconId(data.IconId);
    }

    private class IconInfo {
      public BitmapImage Image { get; set; }
      public Logic.Riot.Platform.Icon Icon { get; set; }
    }
  }
}
