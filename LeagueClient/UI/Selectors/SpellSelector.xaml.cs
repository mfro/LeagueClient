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
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for SpellSelector.xaml
  /// </summary>
  public partial class SpellSelector : UserControl {
    public IEnumerable<SpellDto> Spells {
      get { return _spells; }
      set {
        _spells = value;
        UpdateList();
      }
    }

    public event EventHandler<SpellDto> SpellSelected;

    private IEnumerable<SpellDto> _spells;
    public SpellSelector() {
      InitializeComponent();
    }

    private void UpdateList() {
      var images = new List<object>();
      foreach (var item in _spells) {
        images.Add(new { Image = DataDragon.GetSpellImage(item).Load(), Name = item.name, Data = item });
      }
      SpellGrid.ItemsSource = images;
    }

    private void Spell_Click(object sender, MouseButtonEventArgs e) {
      var data = (((dynamic) sender).DataContext).Data as SpellDto;
      if (SpellSelected != null) SpellSelected(this, data);
    }
  }
}
