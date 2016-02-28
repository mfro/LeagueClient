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
using LeagueClient.Logic;
using MFroehlich.League.Assets;
using RiotClient.Riot.Platform;
using RiotClient;
using RiotClient.Lobbies;

namespace LeagueClient.UI.Client.Lobbies {
  /// <summary>
  /// Interaction logic for DefaultPlayer.xaml
  /// </summary>
  public partial class LobbyPlayer : UserControl, INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public CustomLobbyMember Member { get; }

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

    public LobbyPlayer(CustomLobbyMember member) {
      InitializeComponent();

      this.Member = member;
      member.Changed += (s, e) => Update();
      Update();

      forceExpand = true;
      if (!forceExpand) {
        Image.Height = Image.Width = 0;
        LevelText.Height = 0;
      }
    }

    private void Update() {
      if (Member.IsBot) {
        var champ = DataDragon.ChampData.Value.data[Member.Name.Split('_')[1]];
        SummonerIcon = DataDragon.GetChampIconImage(champ).Load();
        UserName = champ.name;
      } else {
        Session.Current.SummonerCache.GetData(Member.Name, GotSummonerData);
        UserName = Member.Name;
      }
    }

    private void GotSummonerData(SummonerCache.Item item) {
      if (item.Data != null) {
        SummonerIcon = DataDragon.GetProfileIconImage(DataDragon.GetIconData(item.Data.Summoner.ProfileIconId)).Load();
        LevelString = "Level " + item.Data.SummonerLevel.Level;
        UserName = item.Data.Summoner.Name;
      }

      if (item.Leagues != null) {
        var league = item.Leagues.SummonerLeagues.FirstOrDefault(l => l.Queue.Equals(QueueType.RANKED_SOLO_5x5.Key));
        if (league != null) {
          RankString = RankedTier.Values[league.Tier] + " " + league.Rank;
        }
      }
    }

    private void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string name = null) {
      if (!(field?.Equals(value) ?? false)) {
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
      }
    }

    private void Expand() {
      var img = new DoubleAnimation(52, new Duration(TimeSpan.FromMilliseconds(200)));
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
      if (!ForceExpand) Contract();
    }
  }
}
