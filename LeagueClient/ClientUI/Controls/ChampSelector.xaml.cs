using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MyChampDto = MFroehlich.League.DataDragon.ChampionDto;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ChampSelector.xaml
  /// </summary>
  public partial class ChampSelector : UserControl {
    public event EventHandler<MyChampDto> ChampSelected;
    public event EventHandler<MyChampDto.SkinDto> SkinSelected;
    public MyChampDto SelectedChampion { get; set; }
    public MyChampDto.SkinDto SelectedSkin { get; set; }

    public bool IsReadOnly {
      get { return readOnly; }
      set {
        if (readOnly != value) {
          ChampSelect.Visibility = Visibility.Visible;
          SkinSelect.Visibility = Visibility.Hidden;
          readOnly = value;
        }
      }
    }

    private IEnumerable<int> last;

    private List<MyChampDto> champs;
    private bool readOnly;
    private List<MyChampDto.SkinDto> skins = new List<MyChampDto.SkinDto>();

    public ChampSelector() {
      InitializeComponent();
      SkinScroll.ScrollToHorizontalOffset(290);
      if (Client.Connected)
        UpdateChampList();
    }


    public async void UpdateChampList() {
      var champs = new List<MyChampDto>();
      foreach (var riot in await RiotServices.InventoryService.GetAvailableChampions()) {
        if ((!riot.Owned && !riot.FreeToPlay) || riot.Banned) continue;
        champs.Add(LeagueData.GetChampData(riot.ChampionId));
      }
      SetChampList(champs);
    }

    public void SetChampList(IEnumerable<MyChampDto> champions) {
      champs = champions.ToList();
      var images = new List<object>();
      var save = new List<int>();
      var filter = SearchBox.Text;
      if (filter.Equals("Search")) filter = "";
      foreach (var item in champions.OrderBy(c => c.name).Where(c => Regex.IsMatch(c.name, filter, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace))) {
        images.Add(new { Image = LeagueData.GetChampIconImage(item.id), Name = item.name, Data = item });
        save.Add(item.key);
      }
      if (last != null && save.SequenceEqual(last)) return;
      last = save;
      ChampsGrid.ItemsSource = images;
      ChampSelect.Visibility = Visibility.Visible;
      SkinSelect.Visibility = Visibility.Collapsed;
    }

    private void UpdateSkinList() {
      var images = new List<object>();
      foreach (var item in skins) {
        images.Add(new {
          Image = LeagueData.GetChampLoadingImage(SelectedChampion.id+"_"+item.num),
          Name = item.name, Data = item });
      }
      SkinsGrid.ItemsSource = images;
      SkinScroll.ScrollToHorizontalOffset(294);
    }

    private void SearchBox_KeyUp(object sender, KeyEventArgs e) {
      last = null;
      SetChampList(champs);
    }

    private void Champion_Select(object sender, MouseButtonEventArgs e) {
      if (IsReadOnly) return;
      var src = sender as Border;
      var data = (((dynamic) src).DataContext).Data
        as MyChampDto;
      if (data != SelectedChampion) {
        foreach (var item in ChampsGrid.Items)
          VisualTreeHelper
            .GetChild(ChampsGrid.ItemContainerGenerator.ContainerFromItem(item), 0)
            .SetValue(Border.BorderBrushProperty, App.ForeBrush);

        src.BorderBrush = App.FocusBrush;
        if (data == null) return;
        if (ChampSelected != null) ChampSelected(this, data);
        SelectedChampion = data;
      }

      var riotDto = Client.RiotChampions.FirstOrDefault(c => c.ChampionId == data.key);
      skins.Clear();
      foreach (var item in SelectedChampion.skins) {
        var riot = riotDto.ChampionSkins.FirstOrDefault(s => s.SkinId == item.id);
        if (item.num == 0 || riot.Owned) skins.Add(item);
      }
      if (skins.Count == 1) {
        if (SkinSelected != null) SkinSelected(this, skins[0]);
        SelectedSkin = skins[0];
        return;
      }
      UpdateSkinList();

      ChampSelect.Visibility = Visibility.Hidden;
      SkinSelect.Visibility = Visibility.Visible;
      //SkinsButt.IsEnabled = false;
      SkinScroll.ScrollToHorizontalOffset(294);
    }

    private void Skin_Select(object sender, MouseButtonEventArgs e) {
      var src = sender as Border;

      foreach (var item in SkinsGrid.Items) {
        var child =
        VisualTreeHelper.GetChild(SkinsGrid.ItemContainerGenerator.ContainerFromItem(item), 0);
        child.SetValue(Border.BorderBrushProperty, App.ForeBrush);
      }

      src.BorderBrush = App.FocusBrush;
      var data = (((dynamic) src).DataContext).Data
        as MyChampDto.SkinDto;
      if (data == null) return;
      if (SkinSelected != null) SkinSelected(this, data);
      SelectedSkin = data;
    }

    private void SkinGrid_Scroll(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0) SkinScroll.LineLeft();
      else SkinScroll.LineRight();
      e.Handled = true;
    }

    private void ChampButt_Click(object sender, RoutedEventArgs e) {
      ChampSelect.Visibility = Visibility.Visible;
      SkinSelect.Visibility = Visibility.Hidden;
    }
  }
}
