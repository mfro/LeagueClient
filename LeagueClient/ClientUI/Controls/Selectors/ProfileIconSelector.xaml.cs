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
using MFroehlich.League.DataDragon;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ProfileIconSelector.xaml
  /// </summary>
  public partial class ProfileIconSelector : UserControl {
    public event EventHandler<Logic.Riot.Platform.Icon> IconSelected;

    public ProfileIconSelector() {
      InitializeComponent();

      if (Client.Session.Connected)
        LoadIcons();
    }

    public async void LoadIcons() {
      var icons = await RiotServices.SummonerIconService.GetSummonerIconInventory(Client.Session.LoginPacket.AllSummonerData.Summoner.SummonerId);
      var data = new List<object>();
      foreach (var icon in icons.SummonerIcons.OrderByDescending(i => i.PurchaseDate)) {
        ProfileIconDto info;
        if (LeagueData.IconData.Value.data.TryGetValue(icon.IconId.ToString(), out info)) {
          var image = LeagueData.GetProfileIconImage(info);
          if (image != null)
            data.Add(new IconInfo { Image = image, Icon = icon });
        }
      }
      Dispatcher.Invoke(() => IconsGrid.ItemsSource = data);
    }

    private void Icon_Click(object sender, MouseButtonEventArgs e) {
      var data = ((sender as Border).DataContext as IconInfo).Icon;
      Client.Session.LoginPacket.AllSummonerData.Summoner.ProfileIconId = Client.Session.Settings.ProfileIcon = data.IconId;
      IconSelected?.Invoke(this, data);
      RiotServices.SummonerService.UpdateProfileIconId(data.IconId);
    }

    private class IconInfo {
      public BitmapImage Image { get; set; }
      public Logic.Riot.Platform.Icon Icon { get; set; }
    }
  }
}
