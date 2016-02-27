using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using LeagueClient.Logic;
using System.Windows.Threading;
using RiotClient.Riot.Platform;
using RiotClient;

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for MasteryEditor.xaml
  /// </summary>
  public partial class MasteryEditor : UserControl {
    private const int
      HorizontalSpace = 15,
      VerticalSpace = 18;

    public MasteryEditor() {
      InitializeComponent();
      ferocity = new MasteryTree(OffenseTree);
      cunning = new MasteryTree(DefenseTree);
      resolve = new MasteryTree(UtilityTree);

      if (Session.Current.Connected) {
        CreateTree(ferocity, DataDragon.MasteryData.Value.tree.Ferocity, 1);
        CreateTree(cunning, DataDragon.MasteryData.Value.tree.Cunning, 2);
        CreateTree(resolve, DataDragon.MasteryData.Value.tree.Resolve, 3);

        Reset();
      }
    }

    public void Reset() {
      PageList.ItemsSource = Session.Current.Account.Masteries.BookPages;
      var page = Session.Current.Account.Masteries.BookPages.FirstOrDefault(p => p.Current);
      if (page == null) LoadPage(Session.Current.Account.Masteries.BookPages[0]);
      else LoadPage(page);
    }

    public void Save() {
      if (!unsaved) return;
      page.TalentEntries.Clear();
      foreach (var icon in Icons.Values) {
        if (icon.Points == 0) continue;
        page.TalentEntries.Add(new TalentEntry {
          Rank = icon.Points,
          TalentId = icon.Data.id
        });
      }
      Session.Current.Account.SaveMasteries();
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
      CheckPoints();
      unsaved = false;
      Changed.Text = "";
      Session.Current.Account.SelectMasteryPage(page);
    }

    private MasteryTree ferocity;
    private MasteryTree cunning;
    private MasteryTree resolve;
    private int usedPoints;
    private Dictionary<string, MasteryIcon> Icons = new Dictionary<string, MasteryIcon>();

    private void CheckPoints() {
      if (loading) return;
      usedPoints = 0;
      int total = ferocity.Points + cunning.Points + resolve.Points;
      for (int r = 0; r < 6; r++) {
        foreach (var tree in new[] { ferocity, cunning, resolve }) {
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
      OffenseTotal.Text = "Offense: " + ferocity.Points;
      DefenseTotal.Text = "Defense: " + cunning.Points;
      UtilityTotal.Text = "Utility: " + resolve.Points;
      PointStatus.Text = "Available Points: " + (30 - usedPoints);
    }

    private void CreateTree(MasteryTree tree, List<List<MasteryTreeDto.BranchDto>> src, int col) {
      tree.Control.Rows = src.Count;
      tree.Control.Columns = 1;
      for (int y = 0; y < src.Count; y++) {
        var row = new MasteryRow(y, tree);
        row.Control.Columns = src[y].Count;
        for (int x = 0; x < src[y].Count; x++) {
          var item = src[y][x];
          if (item == null) row.AddBlank();
          else {
            var icon = new MasteryIcon(DataDragon.MasteryData.Value.data[item.masteryId], row);
            icon.MouseEnter += Icon_MouseEnter;
            icon.MouseLeave += Icon_MouseLeave;
            icon.Margin = new Thickness(HorizontalSpace / 2, 0, HorizontalSpace / 2, 0);

            Icons.Add(item.masteryId, icon);
            row.Add(icon);
          }
        }
        tree.Add(row);
      }
      tree.PointChanged += (s, p) => CheckPoints();
      //tree.Control.Height = src.Count * (ImageSize + ImageBorder * 2 + VerticalSpace) + VerticalSpace;
    }

    private void Icon_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) {
      var icon = sender as MasteryIcon;
      var pos = icon.TransformToAncestor(BackGrid).Transform(new Point(0, 0));
      ToolTipGrid.Visibility = Visibility.Visible;
      double x = pos.X + 50;
      if (icon.Row.Tree == resolve)
        x = pos.X - 210;
      if (icon.Row.Row == 5) {
        ToolTipGrid.Margin = new Thickness(x, -1000, -1000, BackGrid.ActualHeight - pos.Y - icon.ActualHeight);
        ToolTipGrid.VerticalAlignment = VerticalAlignment.Bottom;
      } else {
        ToolTipGrid.Margin = new Thickness(x, pos.Y, -1000, -1000);
        ToolTipGrid.VerticalAlignment = VerticalAlignment.Top;
      }

      NameLabel.Content = icon.Data.name;
      if (icon.Data.description.Count == 1 || icon.Points == 0)
        DescriptionBlock.Text = icon.Data.description.First().Replace("<br>", "\n");
      else
        DescriptionBlock.Text = icon.Data.description[icon.Points - 1].Replace("<br>", "\n");
    }

    private void Icon_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) {
      ToolTipGrid.Visibility = Visibility.Collapsed;
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
    private void SaveButt_Click(object sender, RoutedEventArgs e) {
      Save();
    }

    private void ResetButt_Click(object sender, RoutedEventArgs e) {
      loading = true;
      foreach (var item in Icons.Values) item.Points = 0;
      loading = false;
      CheckPoints();
    }

    private void DeleteButt_Click(object sender, RoutedEventArgs e) {
      Session.Current.Account.DeleteMasteryPage(Session.Current.Account.SelectedMasteryPage);
    }

    private void RevertButt_Click(object sender, RoutedEventArgs e) {
      LoadPage(page);
    }
    #endregion

    public class MasteryRow {
      public List<MasteryIcon> Icons { get; } = new List<MasteryIcon>();
      public UniformGrid Control { get; }
      public MasteryTree Tree { get; }
      public int Row { get; }

      public int? Ranks => Icons.FirstOrDefault()?.Data?.ranks;
      public int Points => Icons.Sum(item => item.Points);
      public bool Complete => Points == Ranks;

      public MasteryRow(int row, MasteryTree tree) {
        Control = new UniformGrid {
          Rows = 1,
          Margin = new Thickness(0, VerticalSpace / 2, 0, VerticalSpace / 2),
          HorizontalAlignment = HorizontalAlignment.Center
        };
        Tree = tree;
        Row = row;
      }

      public void Add(MasteryIcon icon) {
        Icons.Add(icon);
        Control.Children.Add(icon);
      }

      public void PointsChanged(MasteryIcon src, int amount) {
        if (Points > Ranks) {
          Icons.FirstOrDefault(item => item != src && item.Points >= Points - Ranks.Value).Points -= Points - Ranks.Value;
        }
        Tree.PointsChanged(src, Points);
      }

      public void AddBlank() {
        Control.Children.Add(new Control {
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
