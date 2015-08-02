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

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for PopupSelector.xaml
  /// </summary>
  public partial class PopupSelector : UserControl {
    public enum Selector {
      Champions, Masteries, Spells
    }

    public event EventHandler Close;

    public Selector CurrentSelector {
      get { return selector; }
      set {
        selector = value;
        switch (value) {
          case Selector.Champions:
            ChampSelector.UpdateChampList();
            MasteryEditor.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Collapsed;
            ChampSelector.Visibility = Visibility.Visible;
            break;
          case Selector.Masteries:
            MasteryEditor.Reset();
            ChampSelector.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Collapsed;
            MasteryEditor.Visibility = Visibility.Visible;
            break;
          case Selector.Spells:
            ChampSelector.Visibility = Visibility.Collapsed;
            MasteryEditor.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Visible;
            break;
        }
      }
    }
    private Selector selector;

    public PopupSelector() {
      InitializeComponent();
    }

    private void Close_Click(object src, EventArgs args) {
      Close?.Invoke(this, args);
    }
  }
}
