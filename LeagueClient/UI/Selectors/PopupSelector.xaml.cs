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

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for PopupSelector.xaml
  /// </summary>
  public partial class PopupSelector : UserControl {
    public enum Selector {
      Champions, Masteries, Runes, Spells, ProfileIcons
    }

    public event EventHandler Close;

    public Selector CurrentSelector {
      get { return selector; }
      set {
        selector = value;
        switch (value) {
          case Selector.Champions:
            MasteryEditor.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Collapsed;
            IconSelector.Visibility = Visibility.Collapsed;
            RuneEditor.Visibility = Visibility.Collapsed;
            ChampSelector.Visibility = Visibility.Visible;
            TitleBlock.Content = "Select a Champion";
            break;
          case Selector.Masteries:
            MasteryEditor.Reset();
            ChampSelector.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Collapsed;
            IconSelector.Visibility = Visibility.Collapsed;
            RuneEditor.Visibility = Visibility.Collapsed;
            MasteryEditor.Visibility = Visibility.Visible;
            TitleBlock.Content = "Select Masteries";
            break;
          case Selector.Spells:
            ChampSelector.Visibility = Visibility.Collapsed;
            MasteryEditor.Visibility = Visibility.Collapsed;
            IconSelector.Visibility = Visibility.Collapsed;
            RuneEditor.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Visible;
            TitleBlock.Content = "Select Summoner Spells";
            break;
          case Selector.ProfileIcons:
            ChampSelector.Visibility = Visibility.Collapsed;
            MasteryEditor.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Collapsed;
            IconSelector.Visibility = Visibility.Visible;
            TitleBlock.Content = "Select Summoner Icon";
            break;
          case Selector.Runes:
            ChampSelector.Visibility = Visibility.Collapsed;
            MasteryEditor.Visibility = Visibility.Collapsed;
            SpellSelector.Visibility = Visibility.Collapsed;
            IconSelector.Visibility = Visibility.Collapsed;
            RuneEditor.Visibility = Visibility.Visible;
            TitleBlock.Content = "Select Runes";
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
