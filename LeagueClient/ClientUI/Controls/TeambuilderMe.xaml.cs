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
using LeagueClient.Logic;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
using static LeagueClient.Logic.Strings;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderMe.xaml
  /// </summary>
  public partial class TeambuilderMe : UserControl {
    public event Action<bool> ReadyStateChanged;
    public event Action ChampClicked;
    public event Action Spell1Clicked;
    public event Action Spell2Clicked;
    public event Action MasteryClicked;
    public event Action PositionChanged;

    public SpellDto Spell1 {
      get { return spell1; }
      set {
        Spell1Image.Source = LeagueData.GetSpellImage(value.id);
        spell1 = value;
        UpdateReadyState();
      }
    }
    public SpellDto Spell2 {
      get { return spell2; }
      set {
        Spell2Image.Source = LeagueData.GetSpellImage(value.id);
        spell2 = value;
        UpdateReadyState();
      }
    }
    public ChampionDto Champ {
      get { return champ; }
      set {
        ChampionImage.Source = LeagueData.GetChampIconImage(value.id);
        champ = value;
        UpdateReadyState();
      }
    }
    public ChampionDto.SkinDto Skin { get; set; }
    public Position Position { get; private set; }
    public Role Role { get; private set; }

    private ChampionDto champ;
    private SpellDto spell1;
    private SpellDto spell2;

    public TeambuilderMe() {
      InitializeComponent();
      PositionBox.ItemsSource = Strings.Position.Values.Values; //new string[] { "Top Lane", "Middle Lane", "Bottom Lane", "Jungle" };
      RoleBox.ItemsSource = Strings.Role.Values.Values; //new string[] { "Assassin", "Fighter", "Mage", "Marksman", "Support", "Tank" };
      RunesBox.ItemsSource = Client.LoginPacket.AllSummonerData.SpellBook.BookPages;
      MasteriesBox.ItemsSource = Client.LoginPacket.AllSummonerData.MasteryBook.BookPages;
    }

    public void UpdateMasteries() {
      MasteriesBox.ItemsSource = Client.LoginPacket.AllSummonerData.MasteryBook.BookPages;
    }

    private void UpdateReadyState() {
      bool ready = true;
      if (Champ == null || Skin == null || Spell1 == null || Spell2 == null) ready = false;
      if (PositionBox.SelectedIndex < 0 || RoleBox.SelectedIndex < 0
        || RunesBox.SelectedIndex < 0 || MasteriesBox.SelectedIndex < 0)
        ready = false;
      if (ReadyStateChanged != null) ReadyStateChanged(ready);
    }

    private void Champion_Click(object src, EventArgs args) {
      if (ChampClicked != null) ChampClicked();
    }

    private void Spell1_Click(object src, EventArgs args) {
      if (Spell1Clicked != null) Spell1Clicked();
    }

    private void Spell2_Click(object src, EventArgs args) {
      if (Spell2Clicked != null) Spell2Clicked();
    }

    private void Button_Click(object src, EventArgs args) {
      if (MasteryClicked != null) MasteryClicked();
    }

    private void Box_Changed(object sender, SelectionChangedEventArgs e) {
      if (sender == PositionBox) {
        Position = (Position) PositionBox.SelectedItem;
        if (PositionChanged != null) PositionChanged();
      }
      if (sender == RoleBox) Role = (Role) RoleBox.SelectedItem;
      UpdateReadyState();
    }
  }
}
