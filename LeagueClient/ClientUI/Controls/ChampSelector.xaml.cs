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
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using MyChampDto = MFroehlich.League.DataDragon.ChampionDto;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for ChampSelector.xaml
  /// </summary>
  public partial class ChampSelector : UserControl {
    public IEnumerable<MyChampDto> Champions {
      get {
        return _champs;
      }
      set {
        _champs = value;
        Dispatcher.Invoke(UpdateChampList);
      }
    }

    public event EventHandler<MyChampDto> ChampSelected;
    public event EventHandler<MyChampDto.SkinDto> SkinSelected;
    public MyChampDto SelectedChampion { get; set; }
    public MyChampDto.SkinDto SelectedSkin { get; set; }

    private IEnumerable<MyChampDto> _champs = new List<MyChampDto>();

    private List<MyChampDto.SkinDto> skins = new List<MyChampDto.SkinDto>();
    public ChampSelector() {
      InitializeComponent();
      SkinScroll.ScrollToHorizontalOffset(290);
    }

    public void UpdateChampList() {
      var images = new List<object>();
      foreach (var item in _champs) {
        images.Add(new { Image = LeagueData.GetChampIconImage(item.id), Name = item.name, Data = item });
      }
      ChampsGrid.ItemsSource = images;
      ChampSelect.Visibility = System.Windows.Visibility.Visible;
      SkinSelect.Visibility = System.Windows.Visibility.Collapsed;
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

    private void Champion_Select(object sender, MouseButtonEventArgs e) {
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

      var riotDto = (from c in Client.RiotChampions
                     where c.ChampionId == data.key
                     select c).FirstOrDefault();
      skins.Clear();
      foreach (var item in SelectedChampion.skins) {
        var riot = (from s in riotDto.ChampionSkins
                    where s.SkinId == item.id
                    select s).FirstOrDefault();
        if (item.num == 0 || riot.Owned) skins.Add(item);
      }
      if (skins.Count == 1) {
        if (SkinSelected != null) SkinSelected(this, skins[0]);
        SelectedSkin = skins[0];
        return;
      }
      UpdateSkinList();

      ChampSelect.Visibility = System.Windows.Visibility.Collapsed;
      SkinSelect.Visibility = System.Windows.Visibility.Visible;
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
      RiotCalls.CapService.UpdateLastSelectedSkin(data.id, SelectedChampion.key);
      SelectedSkin = data;
    }

    private void SkinGrid_Scroll(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0) SkinScroll.LineLeft();
      else SkinScroll.LineRight();
      e.Handled = true;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) {
      ChampSelect.Visibility = System.Windows.Visibility.Visible;
      SkinSelect.Visibility = System.Windows.Visibility.Collapsed;
    }
  }
}
