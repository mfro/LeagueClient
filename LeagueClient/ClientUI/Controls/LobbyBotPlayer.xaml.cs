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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for DefaultPlayer.xaml
  /// </summary>
  public partial class LobbyBotPlayer : UserControl, INotifyPropertyChanged, ICollapsable {
    public event PropertyChangedEventHandler PropertyChanged;

    public BitmapImage ChampIcon {
      get { return champIcon; }
      set { SetField(ref champIcon, value); }
    }
    public string UserName {
      get { return userName; }
      set { SetField(ref userName, value); }
    }
    public string Difficulty {
      get { return difficulty; }
      set { SetField(ref difficulty, value); }
    }

    public bool ForceExpand {
      get { return forceExpand; }
      set {
        if (value) Expand();
        else if (!IsMouseOver) Contract();
        
        SetField(ref forceExpand, value);
      }
    }

    private BitmapImage champIcon;
    private string userName;
    private string difficulty;

    private bool forceExpand;

    public LobbyBotPlayer(BotParticipant bot, bool expanded) {
      var champ = LeagueData.ChampData.Value.data[bot.SummonerInternalName.Split('_')[1]];
      ChampIcon = LeagueData.GetChampIconImage(champ);
      UserName = champ.name;
      Difficulty = bot.BotSkillLevelName;

      InitializeComponent();

      forceExpand = expanded;
      if (!expanded) {
        Image.Height = Image.Width = 0;
      }
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }

    private void Expand() {
      var img = new DoubleAnimation(48, new Duration(TimeSpan.FromMilliseconds(200)));
      var lvlHeight = new DoubleAnimation(20, new Duration(TimeSpan.FromMilliseconds(200)));

      Image.BeginAnimation(Image.HeightProperty, img);
      Image.BeginAnimation(Image.WidthProperty, img);
    }

    private void Contract() {
      var anim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200)));

      Image.BeginAnimation(Image.HeightProperty, anim);
      Image.BeginAnimation(Image.WidthProperty, anim);
    }

    private void This_MouseEnter(object sender, MouseEventArgs e) {
      Expand();
    }

    private void This_MouseLeave(object sender, MouseEventArgs e) {
      if(!ForceExpand) Contract();
    }
  }
}
