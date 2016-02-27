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
using System.Windows.Threading;
using LeagueClient.Logic;
using MFroehlich.League.Assets;
using RiotClient.Riot.Platform;
using RiotClient;

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for RuneEditor.xaml
  /// </summary>
  public partial class RuneEditor : UserControl {

    private SpellBookPageDTO page;
    private List<SlotEntry> unedited;
    private List<SummonerRune> runes;
    private bool unsaved;

    public RuneEditor() {
      InitializeComponent();
      CreateList();

      if (Session.Current.Connected)
        Reset();
    }

    public async void Reset() {
      PageList.ItemsSource = Session.Current.Account.Runes.BookPages;
      runes = await Session.Current.Account.GetRuneInventory();
      LoadPage(Session.Current.Account.SelectedRunePage);
    }

    public void Save() {
      if (!unsaved) return;
      Session.Current.Account.SaveRunes();
      Dispatcher.Invoke(() => Changed.Text = "");
      unsaved = false;
    }

    private void LoadPage(SpellBookPageDTO page) {
      this.page = page;
      unedited = new List<SlotEntry>(page.SlotEntries);
      Changed.Text = "";
      unsaved = false;
      PageNameBox.Text = page.Name;
      UpdateRunes();
      Session.Current.Account.SelectRunePage(page);
    }

    private void UpdateRunes() {
      if (!DataDragon.IsCurrent) return;
      int i = 0;
      for (; i < 9; i++) Runes[i].Source = new BitmapImage(new Uri("pack://application:,,,/RiotAPI;component/Resources/MarkDefault.png"));
      for (; i < 18; i++) Runes[i].Source = new BitmapImage(new Uri("pack://application:,,,/RiotAPI;component/Resources/SealDefault.png"));
      for (; i < 27; i++) Runes[i].Source = new BitmapImage(new Uri("pack://application:,,,/RiotAPI;component/Resources/GlyphDefault.png"));
      for (; i < 30; i++) Runes[i].Source = new BitmapImage(new Uri($"pack://application:,,,/RiotAPI;component/Resources/Quint{i - 26}Default.png"));

      foreach (var entry in page.SlotEntries) {
        var rune = Runes[entry.RuneSlotId - 1];
        var data = DataDragon.RuneData.Value.data[entry.rune.ItemId.ToString()];
        rune.Source = DataDragon.GetRuneImage(data).Load();
        rune.Tag = entry;
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var title = new Label { HorizontalAlignment = HorizontalAlignment.Center, Content = data.name };
        var detail = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, Text = data.description };
        grid.Children.Add(title);
        grid.Children.Add(detail);
        Grid.SetRow(detail, 1);
        rune.ToolTip = grid;
      }

      MarksList.Children.Clear();
      SealsList.Children.Clear();
      GlyphsList.Children.Clear();
      QuintsList.Children.Clear();
      foreach (var rune in runes) {
        var data = DataDragon.RuneData.Value.data[rune.RuneId.ToString()];
        int used = page.SlotEntries.Count(r => r.RuneId == rune.RuneId);
        if (rune.Quantity - used > 0) {
          var item = new RuneListItem(rune, rune.Quantity - used);
          item.MouseUp += RuneListItem_Click;
          switch (data.rune.type) {
            case "red":
              MarksList.Children.Add(item);
              break;
            case "yellow":
              SealsList.Children.Add(item);
              break;
            case "blue":
              GlyphsList.Children.Add(item);
              break;
            case "black":
              QuintsList.Children.Add(item);
              break;
          }
        }
      }
    }

    private void RuneListItem_Click(object sender, MouseButtonEventArgs e) {
      var rune = ((RuneListItem) sender).Rune;
      int start = -1;
      int end = -1;
      switch (rune.Rune.RuneType.Name) {
        case "Red":
          start = 1; end = 10;
          break;
        case "Yellow":
          start = 10; end = 19;
          break;
        case "Blue":
          start = 19; end = 28;
          break;
        case "Black":
          start = 28; end = 31;
          break;
      }
      for (int i = start; i < end; i++) {
        if (page.SlotEntries.Any(s => s.RuneSlotId == i)) continue;
        page.SlotEntries.Add(new SlotEntry {
          rune = rune.Rune,
          RuneId = rune.RuneId,
          runeSlot = new RuneSlot { Id = i, RuneType = rune.Rune.RuneType },
          RuneSlotId = i
        });
        unsaved = true;
        Changed.Text = "*Unsaved*";
        break;
      }
      UpdateRunes();
    }

    private void Rune_Click(object src, EventArgs args) {
      var rune = ((Image) src).Tag as SlotEntry;
      page.SlotEntries.Remove(rune);
      unsaved = true;
      Changed.Text = "*Unsaved*";
      UpdateRunes();
    }

    private List<Image> Runes;
    private void CreateList() {
      Runes = new List<Image> {
        Red1, Red2, Red3, Red4, Red5, Red6, Red7, Red8, Red9,
        Yellow1, Yellow2, Yellow3, Yellow4, Yellow5, Yellow6, Yellow7, Yellow8, Yellow9,
        Blue1, Blue2, Blue3, Blue4, Blue5, Blue6, Blue7, Blue8, Blue9,
        Quint1, Quint2, Quint3
      };
    }

    #region Event Listeners

    private void PageList_MouseWheel(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0) BookScroll.LineLeft();
      else BookScroll.LineRight();
      e.Handled = true;
    }

    private void Page_Open(object sender, RoutedEventArgs e) {
      var page = ((Button) sender).DataContext as SpellBookPageDTO;
      if (page != null) LoadPage(page);
    }

    private void SaveButt_Click(object sender, RoutedEventArgs e) => Save();

    private void ResetButt_Click(object sender, RoutedEventArgs e) {
      page.SlotEntries.Clear();
      Changed.Text = "*Unsaved*";
      unsaved = true;
      UpdateRunes();
    }

    private void RevertButt_Click(object sender, RoutedEventArgs e) {
      page.SlotEntries = new List<SlotEntry>(unedited);
      Changed.Text = "";
      unsaved = false;
      UpdateRunes();
    }

    #endregion
  }
}
