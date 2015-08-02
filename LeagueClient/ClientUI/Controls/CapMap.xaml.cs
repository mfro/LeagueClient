using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LeagueClient.Logic.Cap;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderMap.xaml
  /// </summary>
  public partial class CapMap : UserControl {
    private static List<PointRef> TopLane = new List<PointRef> {
      new PointRef { { .083, .083 } },
      new PointRef { { .040, .133 }, { .133, .050 } },
      new PointRef { { .023, .200 }, { .083, .083 }, { .200, .023 } },
      new PointRef { { .016, .016 }, { .150, .016 }, { .150, .150 }, { .016, .150 } },
      new PointRef { { .016, .016 }, { .196, .016 }, { .196, .196 }, { .016, .196 }, { .106, .106 } },
    };
    private static List<PointRef> MidLane = new List<PointRef> {
      new PointRef { { .442, .442 } },
      new PointRef { { .394, .490 }, { .490, .394 } },
      new PointRef { { .385, .480 }, { .450, .370 }, { .515, .480 } },
      new PointRef { { .385, .375 }, { .515, .375 }, { .515, .505 }, { .385, 505 } },
      new PointRef { { .360, .360 }, { .538, .360 }, { .538, .538 }, { .360, .538 }, { .448, .448 } },
    };
    private static List<PointRef> BotLane = new List<PointRef> {
      new PointRef { { .800, .790 } },
      new PointRef { { .748, .830 }, { .848, .750 } },
      new PointRef { { .809, .715 }, { .748, .820 }, { .870, .820 } },
      new PointRef { { .748, .598 }, { .870, .598 }, { .870, .820 }, { .748, .820 } },
      new PointRef { { .698, .648 }, { .870, .648 }, { .870, .820 }, { .698, .820 }, { .784, .734 } },
    };
    private static List<PointRef> Jungle = new List<PointRef> {
      new PointRef { { .250, .300 } },
      new PointRef { { .250, .300 }, { .650, .600 } },
      new PointRef { { .200, .260 }, { .650, .600 }, { .300, .340 } },
      new PointRef { { .200, .984 }, { .600, .560 }, { .300, .340 }, { .700, .640 } },
      new PointRef { { .190, .340 }, { .600, .560 }, { .320, .340 }, { .700, .640 }, { .255, .230 } },
    };
    public BindingList<CapPlayer> Players { get; private set; }
    private List<CapPlayer> subscribed = new List<CapPlayer>();

    public CapMap() {
      Players = new BindingList<CapPlayer>();
      Players.ListChanged += Players_ListChanged;
      InitializeComponent();

      GameMap.Source = LeagueData.GetMapImage(11); // Summoner's Rift
    }

    void Players_ListChanged(object sender, ListChangedEventArgs e) {
      switch (e.ListChangedType) {
        case ListChangedType.ItemAdded:
          if (!subscribed.Contains(Players[e.NewIndex])) {
            Players[e.NewIndex].PropertyChanged += CapMap_PlayerUpdate;
            subscribed.Add(Players[e.NewIndex]);
          }
          break;
        case ListChangedType.Reset:
          foreach (var item in Players.Where(p => !subscribed.Contains(p)))
            item.PropertyChanged += CapMap_PlayerUpdate;
          break;
      }
      CapMap_PlayerUpdate(null, null);
    }

    private void CapMap_PlayerUpdate(object sender, EventArgs e) {
      Dispatcher.Invoke(Reset);
    }

    private void Reset() {
      int topT = (from p in Players where p.Position == Position.TOP && p.Champion != null select p).Count();
      int midT = (from p in Players where p.Position == Position.MIDDLE && p.Champion != null select p).Count();
      int botT = (from p in Players where p.Position == Position.BOTTOM && p.Champion != null select p).Count();
      int jglT = (from p in Players where p.Position == Position.JUNGLE && p.Champion != null select p).Count();
      int top = 0, mid = 0, bot = 0, jgl = 0;
      Body.Children.Clear();
      foreach (var item in Players.Where(p => p.Position != null && p.Champion != null)) {
        Point point;
        if (item.Position == Position.TOP) point = TopLane[topT - 1][top++];
        else if (item.Position == Position.MIDDLE) point = MidLane[midT - 1][mid++];
        else if (item.Position == Position.BOTTOM) point = BotLane[botT - 1][bot++];
        else if (item.Position == Position.JUNGLE) point = Jungle[jglT - 1][jgl++];
        else continue;

        var img = new Image {
          Width = ActualWidth * .12,
          Height = ActualHeight * .12,
          Style = (Style) FindResource("Image"),
          Margin = new Thickness(ActualWidth * point.X, ActualHeight * point.Y, 0, 0)
        };
        img.Source = LeagueData.GetChampIconImage(item.Champion.id);
        img.Clip = new EllipseGeometry {
          RadiusX = img.Width / 2,
          RadiusY = img.Height / 2,
          Center = new Point(img.Width / 2, img.Height / 2),
        };
        Body.Children.Add(img);
      }
    }

    private class PointRef : List<Point> {
      public void Add(double x, double y) {
        Add(new Point(x, y));
      }
    }
  }
}
