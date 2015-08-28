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
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic.Chat;
using LeagueClient.Logic.Riot;
using LeagueClient.Logic.Riot.Platform;
using MFroehlich.League.DataDragon;
using RtmpSharp.Messaging;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for ChampSelect.xaml
  /// </summary>
  public partial class ChampSelectPage : Page, IClientPage {

    private const string
      YouPickString = "Your turn to pick!";

    private ChatRoomController chatRoom;

    public ChampSelectPage(GameDTO game) {
      InitializeComponent();
      chatRoom = new ChatRoomController(ChatBox, ChatHistory, ChatButt, ChatScroller);
      chatRoom.JoinChat(new jabber.JID(game.RoomName + ".pvp.net"), game.RoomPassword);

      ChampsGrid.ChampSelected += ChampsGrid_ChampSelected;
      ChampsGrid.SkinSelected += ChampsGrid_SkinSelected;
      GotGameData(game);
    }

    public bool HandleMessage(MessageReceivedEventArgs args) {
      GameDTO game;
      if ((game = args.Body as GameDTO) != null) {
        Client.Invoke(GotGameData, game);
        return true;
      }

      return false;
    }

    public void GotGameData(GameDTO game) {
      if (game.GameState.Equals("CHAMP_SELECT") || game.GameState.Equals("PRE_CHAMP_SELECT")) {
        Dispatcher.Invoke(() => {
          var readOnly = true;
          MyTeam.Children.Clear();
          OtherTeam.Children.Clear();
          foreach (var thing in game.TeamOne.Concat(game.TeamTwo)) {
            var player = thing as PlayerParticipant;
            bool blue = game.TeamOne.Contains(player);
            var bot = thing as BotParticipant;
            UIElement control;
            if (player != null) {
              control = new ChampSelectPlayer(player, game.PlayerChampionSelections.FirstOrDefault(c => c.SummonerInternalName.Equals(player.SummonerInternalName)));
              if(player.PickTurn == game.PickTurn && player.SummonerId == Client.LoginPacket.AllSummonerData.Summoner.SumId) {
                GameStatusLabel.Content = YouPickString;
                readOnly = false;
              }
            } else if (bot != null)
              control = new ChampSelectPlayer(bot);
            else throw new NotImplementedException(thing.GetType().Name);

            if (blue) {
              MyTeam.Children.Add(control);
            } else {
              OtherTeam.Children.Add(control);
            }
          }
          ChampsGrid.ReadOnly = readOnly;
        });
        Dispatcher.BeginInvoke(new Action(() => MyTeam.Height = OtherTeam.Height = Math.Max(MyTeam.ActualHeight, OtherTeam.ActualHeight)), System.Windows.Threading.DispatcherPriority.Input);
      }
    }

    private void ChampsGrid_ChampSelected(object sender, ChampionDto e) {
      RiotCalls.GameService.SelectChampion(e.key);
    }

    private async void ChampsGrid_SkinSelected(object sender, ChampionDto.SkinDto e) {
      await RiotCalls.GameService.SelectChampionSkin(ChampsGrid.SelectedChampion.key, e.id);
    }

    private void SkinScroller_MouseWheel(object sender, MouseWheelEventArgs e) {
      if (e.Delta > 0)
        ((ScrollViewer) sender).LineLeft();
      else
        ((ScrollViewer) sender).LineRight();
      e.Handled = true;
    }

    private void Border_MouseDown(object sender, MouseButtonEventArgs e) {
      Client.MainWindow.DragMove();
    }
  }
}
