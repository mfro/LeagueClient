using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using LeagueClient.Logic;
using System.Windows.Threading;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for MasteryEditor.xaml
  /// </summary>
  public partial class MasteryEditor : UserControl {
    private const int
      ImageSize = 40,
      ImageBorder = 2,
      HorizontalSpace = 15,
      VerticalSpace = 22;

    public MasteryEditor() {
      InitializeComponent();
      offense = new MasteryTree(OffenseTree);
      defense = new MasteryTree(DefenseTree);
      utility = new MasteryTree(UtilityTree);

      if (Client.Connected) {
        LoadMasteries(offense, LeagueData.MasteryData.Value.tree.Ferocity, 1);
        LoadMasteries(defense, LeagueData.MasteryData.Value.tree.Cunning, 2);
        LoadMasteries(utility, LeagueData.MasteryData.Value.tree.Resolve, 3);

        Reset();
      }
    }

    public void Reset() {
      PageList.ItemsSource = Client.Masteries.BookPages;
      var page = Client.Masteries.BookPages.FirstOrDefault(p => p.Current);
      if (page == null) LoadPage(Client.Masteries.BookPages[0]);
      else LoadPage(page);
    }

    public async Task Save() {
      if (!unsaved) return;
      page.TalentEntries.Clear();
      foreach (var icon in Icons.Values) {
        if (icon.Points == 0) continue;
        page.TalentEntries.Add(new TalentEntry {
          Rank = icon.Points,
          TalentId = icon.Data.id
        });
      }
      await RiotServices.MasteryBookService.SaveMasteryBook(Client.Masteries);
      if (System.Threading.Thread.CurrentThread == Dispatcher.Thread) Changed.Text = "";
      else Dispatcher.Invoke(() => Changed.Text = "");
      unsaved = false;
    }

    private bool loading;
    private bool unsaved;
    private MasteryBookPageDTO page;

    private void LoadPage(MasteryBookPageDTO page) {
      loading = true;
      this.page = page;
      foreach (var item in Icons.Values) item.Points = 0;
      PageNameBox.Text = page.Name;
      foreach (var item in page.TalentEntries) {
        var talent = item;
        Icons[talent.TalentId + ""].Points = talent.Rank;
      }
      loading = false;
      UpdateMasteries();
      unsaved = false;
      Changed.Text = "";
      Client.SelectMasteryPage(page);
    }

    private MasteryTree offense;
    private MasteryTree defense;
    private MasteryTree utility;
    private int usedPoints;
    private Dictionary<string, MasteryIcon> Icons = new Dictionary<string, MasteryIcon>();

    private void UpdateMasteries() {
      if (loading) return;
      usedPoints = 0;
      int total = offense.Points + defense.Points + utility.Points;
      for (int r = 0; r < 6; r++) {
        foreach (var tree in new[] { offense, defense, utility }) {
          foreach (var item in tree.Rows[r].Icons) {
            bool enabled = true;
            if (total == 30 && item.Points == 0)
              enabled = false;
            else if (r > 0 && !tree.Rows[r - 1].Complete)
              enabled = false;
            item.Enabled = enabled;
            usedPoints += item.Points;
          }
        }
      }
      unsaved = true;
      Changed.Text = "*Unsaved*";
      OffenseTotal.Text = "Offense: " + offense.Points;
      DefenseTotal.Text = "Defense: " + defense.Points;
      UtilityTotal.Text = "Utility: " + utility.Points;
      PointStatus.Text = "Available Points: " + (30 - usedPoints);
    }

    private void LoadMasteries(MasteryTree tree, List<List<MasteryTreeDto.BranchDto>> src, int col) {
      tree.Control.Rows = src.Count;
      tree.Control.Columns = 1;
      for (int y = 0; y < src.Count; y++) {
        var row = new MasteryRow(y, tree);
        for (int x = 0; x < src[y].Count; x++) {
          var item = src[y][x];
          if (item == null) row.AddBlank();
          else {
            var icon = new MasteryIcon(LeagueData.MasteryData.Value.data[item.masteryId], row);
            icon.Control.Margin = new Thickness(HorizontalSpace / 2, 0, HorizontalSpace / 2, 0);

            Icons.Add(item.masteryId, icon);
            row.Add(icon);
          }
        }
        tree.Add(row);
      }
      tree.PointChanged += (s, p) => UpdateMasteries();
      //tree.Control.Height = src.Count * (ImageSize + ImageBorder * 2 + VerticalSpace) + VerticalSpace;
    }

    private void ItemsControl_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
      if (e.Delta > 0) BookScroll.LineLeft();
      else BookScroll.LineRight();
      e.Handled = true;
    }

    private void Page_Open(object sender, RoutedEventArgs e) {
      var page = ((FrameworkElement) sender).DataContext as MasteryBookPageDTO;
      if (page != null) LoadPage(page);
    }

    #region Button Event Listeners
    private async void SaveButt_Click(object sender, RoutedEventArgs e) {
      await Save();
    }

    private void ResetButt_Click(object sender, RoutedEventArgs e) {
      loading = true;
      foreach (var item in Icons.Values) item.Points = 0;
      loading = false;
      UpdateMasteries();
    }

    private void DeleteButt_Click(object sender, RoutedEventArgs e) {
      Client.Masteries.BookPages.Remove(Client.SelectedMasteryPage);
    }

    private void RevertButt_Click(object sender, RoutedEventArgs e) {
      LoadPage(page);
    }
    #endregion

    public class MasteryIcon {
      public event EventHandler<int> PointChanged;
      public Border Control { get; private set; }
      public TextBlock PointsLabel { get; private set; }
      public MasteryDto Data { get; private set; }
      public int Points {
        get { return points; }
        set {
          var old = points;
          points = value;
          PointsLabel.Text = points + "/" + Data.ranks;
          if (points == Data.ranks) Control.BorderBrush = App.FocusBrush;
          else Control.BorderBrush = App.ForeBrush;
          if (PointChanged != null && old != value)
            PointChanged(this, value);
        }
      }
      public bool Enabled {
        get { return enabled; }
        set {
          enabled = value;
          image.Source = LeagueData.GetMasteryImage(Data, !enabled);
          if (!enabled && points > 0) Points = 0;
        }
      }

      private int points;
      private Image image;
      private bool enabled;
      private MasteryRow row;

      public MasteryIcon(MasteryDto dto, MasteryRow row) {
        Data = dto;
        var grid = new Grid();
        Control = new Border();
        Control.Width = Control.Height = ImageSize + ImageBorder * 2;
        Control.BorderBrush = App.ForeBrush;
        Control.BorderThickness = new Thickness(ImageBorder);
        Control.Child = grid;
        PointsLabel = new TextBlock { Text = "0/" + Data.ranks };
        PointsLabel.Style = App.Control;
        PointsLabel.Background = App.ForeBrush;
        PointsLabel.Margin = new Thickness(0, 0, -6, -10);
        PointsLabel.VerticalAlignment = VerticalAlignment.Bottom;
        PointsLabel.HorizontalAlignment = HorizontalAlignment.Right;
        PointsLabel.FontSize = 11;
        grid.Children.Add(image = new Image());
        grid.Children.Add(PointsLabel);

        this.row = row;

        Control.MouseWheel += Control_MouseWheel;
      }

      void Control_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
        int d = (e.Delta > 0) ? 1 : -1;
        //If not enabled or removing any points in higher rows
        if (!enabled || (d < 0 && row.Tree.Rows.Any(row => row.Row > this.row.Row && row.Points > 0)))
          return;
        if (points + d <= Data.ranks && points + d >= 0) {
          Points += d;
          row.PointsChanged(this, d);
        }
      }
    }

    public class MasteryRow {
      public List<MasteryIcon> Icons { get; } = new List<MasteryIcon>();
      public StackPanel Control { get; }
      public MasteryTree Tree { get; }
      public int Row { get; }

      public int? Ranks => Icons.FirstOrDefault()?.Data?.ranks;
      public int Points { get; private set; }
      public bool Complete => Points == Ranks;

      public MasteryRow(int row, MasteryTree tree) {
        Control = new StackPanel {
          Margin = new Thickness(0, 0, 0, VerticalSpace),
          Orientation = Orientation.Horizontal,
          HorizontalAlignment = HorizontalAlignment.Center
        };
        Tree = tree;
        Row = row;
      }

      public void Add(MasteryIcon icon) {
        Icons.Add(icon);
        Control.Children.Add(icon.Control);
      }

      public void PointsChanged(MasteryIcon src, int amount) {
        Points = 0;
        foreach (var item in Icons) Points += item.Points;
        if (Points > Ranks) {
          Icons.FirstOrDefault(item => item != src && item.Points >= Points - Ranks.Value).Points -= Points - Ranks.Value;
          Points = Ranks.Value;
        }
        Tree.PointsChanged(src, Points);
      }

      public void AddBlank() {
        Control.Children.Add(new Control {
          Width = ImageSize + ImageBorder * 2,
          Height = ImageSize + ImageBorder * 2,
          Margin = new Thickness(HorizontalSpace / 2, 0, HorizontalSpace / 2, 0)
        });
      }
    }

    public class MasteryTree {
      public event EventHandler<int> PointChanged;
      public List<MasteryRow> Rows { get; } = new List<MasteryRow>();
      public UniformGrid Control { get; }

      public int Points { get; private set; }

      public MasteryTree(UniformGrid grid) {
        Control = grid;
      }

      public void PointsChanged(MasteryIcon src, int amount) {
        Points = 0;
        Points = Rows.Sum(r => r.Points);
        PointChanged?.Invoke(src, Points);
      }

      public void Add(MasteryRow row) {
        Control.Children.Add(row.Control);
        Rows.Add(row);
      }
    }
  }
}
