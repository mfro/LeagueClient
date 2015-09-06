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
  public partial class LobbyPlayer : UserControl, INotifyPropertyChanged, ICollapsable {
    public event PropertyChangedEventHandler PropertyChanged;

    public BitmapImage SummonerIcon {
      get { return summonerIcon; }
      set { SetField(ref summonerIcon, value); }
    }
    public string UserName {
      get { return userName; }
      set { SetField(ref userName, value); }
    }
    public string LevelString {
      get { return levelString; }
      set { SetField(ref levelString, value); }
    }
    public string RankString {
      get { return rankString; }
      set { SetField(ref rankString, value); }
    }

    public bool ForceExpand {
      get { return forceExpand; }
      set {
        if (value) Expand();
        else if (!IsMouseOver) Contract();
        
        SetField(ref forceExpand, value);
      }
    }

    private BitmapImage summonerIcon;
    private string userName;
    private string levelString;
    private string rankString;

    private bool forceExpand;
      
    public LobbyPlayer(PlayerParticipant player, bool expanded) {
      RiotServices.SummonerService.GetSummonerByName(player.SummonerName).ContinueWith(GotSummonerData);
      SummonerIcon = LeagueData.GetProfileIconImage(LeagueData.GetIconData(player.ProfileIconId));
      UserName = player.SummonerName;

      InitializeComponent();

      forceExpand = expanded;
      if (!expanded) {
        Image.Height = Image.Width = 0;
        LevelText.Height = 0;
      }
    }

    public LobbyPlayer(Member member, bool expanded) {
      RiotServices.SummonerService.GetSummonerByName(member.SummonerName).ContinueWith(GotSummonerData);

      InitializeComponent();

      forceExpand = expanded;
      if (!expanded) {
        Image.Height = Image.Width = 0;
        LevelText.Height = 0;
      }
    }

    private void GotSummonerData(Task<PublicSummoner> task) {
      SummonerIcon = LeagueData.GetProfileIconImage(LeagueData.GetIconData(task.Result.ProfileIconId));
      UserName = task.Result.Name;
      LevelString = "Level " + task.Result.SummonerLevel;
      RankString = "Challenjour";
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
      LevelText.BeginAnimation(Label.HeightProperty, lvlHeight);
    }

    private void Contract() {
      var anim = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200)));

      Image.BeginAnimation(Image.HeightProperty, anim);
      Image.BeginAnimation(Image.WidthProperty, anim);
      LevelText.BeginAnimation(Label.HeightProperty, anim);
    }

    private void This_MouseEnter(object sender, MouseEventArgs e) {
      Expand();
    }

    private void This_MouseLeave(object sender, MouseEventArgs e) {
      if(!ForceExpand) Contract();
    }
  }
}
