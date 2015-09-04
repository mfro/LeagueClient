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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for MasteryEditor.xaml
  /// </summary>
  public partial class MasteryEditor : UserControl {
    private const int
      ImageSize = 40,
      ImageBorder = 2,
      HorizontalSpace = 12,
      VerticalSpace = 14;

    public MasteryEditor() {
      InitializeComponent();
      offense = new MasteryTree(OffenseTree);
      defense = new MasteryTree(DefenseTree);
      utility = new MasteryTree(UtilityTree);

      if (Client.Connected) {
        LoadMasteries(offense, LeagueData.MasteryData.Value.tree.Offense, 1);
        LoadMasteries(defense, LeagueData.MasteryData.Value.tree.Defense, 2);
        LoadMasteries(utility, LeagueData.MasteryData.Value.tree.Utility, 3);

        Reset();
      }
    }

    public void Reset() {
      LoadBook(Client.Masteries);
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
      await RiotCalls.MasteryBookService.SaveMasteryBook(Client.Masteries);
      if (System.Threading.Thread.CurrentThread == Dispatcher.Thread) Changed.Text = "";
      else Dispatcher.Invoke(() => Changed.Text = "");
      unsaved = false;
    }

    private bool loading;
    private bool unsaved;
    private MasteryBookPageDTO page;

    private void LoadBook(MasteryBookDTO book) {
      PageList.ItemsSource = book.BookPages;
      var page = book.BookPages.FirstOrDefault(p => p.Current);
      if (page == null) LoadPage(book.BookPages[0]);
      else LoadPage(page);
    }

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

    private bool CanRemove(MasteryIcon src) {
      var trees = new[] { offense, defense, utility };
      MasteryTree tree = null;
      foreach(var treeCheck in trees) {
        var rowCheck = treeCheck.Rows.FirstOrDefault(r => r.Contains(src));
        if (rowCheck != null) {
          tree = treeCheck;
          break;
        }
      }

      for (int i = src.Row + 1; i < tree.Rows.Count; i++) {
        foreach(var icon in tree.Rows[i]) {
          if (icon.Points > 0 && (tree.PointsAboveRow(i) - 1 < 4 * i || icon.Data.prereq == src.Data.id.ToString()))
            return false;
        }
      }

      return true;
    }

    private void UpdateMasteries() {
      if (loading) return;
      usedPoints = 0;
      int total = offense.Points + defense.Points + utility.Points;
      for (int r = 0; r < 6; r++) {
        foreach (var item in offense.Rows[r]) {
          bool prereq;
          if (item.Data.prereq.Equals("0")) prereq = true;
          else {
            var pre = Icons[item.Data.prereq];
            prereq = pre.Points == pre.Data.ranks;
          }
          item.Enabled = (total < 30 || item.Points > 0) && prereq && offense.PointsAboveRow(r) >= 4 * r;
          usedPoints += item.Points;
        }
        foreach (var item in defense.Rows[r]) {
          bool prereq;
          if (item.Data.prereq.Equals("0")) prereq = true;
          else {
            var pre = Icons[item.Data.prereq];
            prereq = pre.Points == pre.Data.ranks;
          }
          item.Enabled = (total < 30 || item.Points > 0) && prereq && defense.PointsAboveRow(r) >= 4 * r;
          usedPoints += item.Points;
        }
        foreach (var item in utility.Rows[r]) {
          bool prereq;
          if (item.Data.prereq.Equals("0")) prereq = true;
          else {
            var pre = Icons[item.Data.prereq];
            prereq = pre.Points == pre.Data.ranks;
          }
          item.Enabled = (total < 30 || item.Points > 0) && prereq && utility.PointsAboveRow(r) >= 4 * r;
          usedPoints += item.Points;
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
      var points = new Dictionary<string, Point>();
      for (int y = 0; y < src.Count; y++) {
        var row = new List<MasteryIcon>();
        for (int x = 0; x < 4; x++) {
          if (x >= src[y].Count) { tree.Control.Children.Add(new Control()); continue; }
          var item = src[y][x];
          if (item == null) tree.Control.Children.Add(new Control());
          else {
            points.Add(item.masteryId, new Point(x, y));
            var icon = new MasteryIcon(LeagueData.MasteryData.Value.data[item.masteryId], y, CanRemove);
            row.Add(icon);
            Icons.Add(item.masteryId, icon);
            tree.Control.Children.Add(icon.Control);
            if (!item.prereq.Equals("0")) {
              double srcY = y * (ImageSize + ImageBorder * 2 + VerticalSpace + 2)
                + VerticalSpace + 3;
              double srcX = x * (ImageSize + ImageBorder * 2 + HorizontalSpace)
                + HorizontalSpace + ImageBorder + ImageSize / 2 + 4.5;
              double dstY = points[item.prereq].Y * (ImageSize + ImageBorder * 2 + VerticalSpace + 2)
                + VerticalSpace + ImageBorder * 2 + ImageSize + 3;
              double dstX = points[item.prereq].X * (ImageSize + ImageBorder * 2 + HorizontalSpace)
                + HorizontalSpace + ImageBorder + ImageSize / 2 + 4.5;
              var path = new Line() { X1 = srcX, Y1 = srcY, X2 = dstX, Y2 = dstY };
              path.SnapsToDevicePixels = true;
              path.Stroke = App.FontBrush;
              path.StrokeThickness = 7;
              BackGrid.Children.Add(path);
              Grid.SetRow(path, 1);
              Grid.SetColumn(path, col);
            }
          }
        }
        tree.AddRow(row);
      }
      tree.PointChanged += (s, p) => UpdateMasteries();
      tree.Control.Height = src.Count * (ImageSize + ImageBorder * 2 + VerticalSpace) + VerticalSpace;
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

    }

    private void RevertButt_Click(object sender, RoutedEventArgs e) {
      LoadPage(page);
    }

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
          image.Source = LeagueData.GetMasteryImage(Data.id, !enabled);
          if (!enabled && points > 0) Points = 0;
        }
      }
      public int Row { get; private set; }

      private int points;
      private Image image;
      private bool enabled;
      private Func<MasteryIcon, bool> CanRemove;

      public MasteryIcon(MasteryDto dto, int row, Func<MasteryIcon, bool> CanRemove) {
        Data = dto;
        Row = row;
        var grid = new Grid();
        Control = new Border();
        Control.Width = Control.Height = ImageSize + ImageBorder * 2;
        Control.BorderBrush = App.ForeBrush;
        Control.BorderThickness = new Thickness(ImageBorder);
        Control.Margin = new Thickness(HorizontalSpace / 2, VerticalSpace / 2,
                                       HorizontalSpace / 2, VerticalSpace / 2);
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

        this.CanRemove = CanRemove;

        Control.MouseWheel += Control_MouseWheel;
      }

      void Control_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) {
        if (!enabled || (e.Delta < 0  && points > 0 && !CanRemove(this))) return;
        int d = (e.Delta > 0) ? 1 : -1;
        if (points + d <= Data.ranks && points + d >= 0)
          Points += d;
      }
    }

    public class MasteryTree {
      public event EventHandler<int> PointChanged;
      public UniformGrid Control { get; private set; }
      public int Points { get; private set; }
      public List<List<MasteryIcon>> Rows { get; private set; }

      public MasteryTree(UniformGrid grid) {
        Control = grid;
        Rows = new List<List<MasteryIcon>>();
      }

      public void AddRow(List<MasteryIcon> icons) {
        foreach (var item in icons)
          item.PointChanged += PointsChanged;
        Rows.Add(icons);
      }

      public int PointsAboveRow(int row) {
        int points = 0;
        for (int i = 0; i < ((row < 6) ? row : 6); i++) {
          foreach (var item in Rows[i]) points += item.Points;
        }
        return points;
      }

      void PointsChanged(object src, int amount) {
        Points = 0;
        foreach (var row in Rows)
          foreach (var item in row)
            Points += item.Points;
        if (PointChanged != null) PointChanged(this, Points);
      }
    }
  }
}
