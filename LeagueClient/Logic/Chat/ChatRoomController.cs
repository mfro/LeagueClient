using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using jabber;

namespace LeagueClient.Logic.Chat {
  public class ChatRoomController {
    private TextBox input;
    private RichTextBox output;
    private Button send;
    private ScrollViewer scroller;

    private jabber.connection.Room chatRoom;
    private Dispatcher dispatch = App.Current.Dispatcher;

    public bool IsJoined { get; private set; }

    public ChatRoomController(TextBox input, RichTextBox output, Button send, ScrollViewer scroller) {
      this.input = input;
      this.output = output;
      this.send = send;
      this.scroller = scroller;

      input.KeyUp += TextBox_KeyUp;
      send.Click += Button_Click;
    }

    public void JoinChat(JID jid, string pass) {
      if (IsJoined) return;
      chatRoom = Client.ChatManager.JoinRoom(jid);
      chatRoom.OnRoomMessage += (s, e) => dispatch.MyInvoke(ShowMessage, chatRoom.Participants[e.From].Nick, e.Body);
      chatRoom.OnParticipantJoin += (s, e) => dispatch.MyInvoke(ShowLobbyMessage, e.Nick + " has joined the lobby");
      chatRoom.OnParticipantLeave += (s, e) => dispatch.MyInvoke(ShowLobbyMessage, e.Nick + " has left the lobby");
      chatRoom.OnJoin += room => dispatch.MyInvoke(ShowLobbyMessage, "Joined chat lobby");
      chatRoom.Join(pass);
      IsJoined = true;
    }

    public void LeaveChat() {
      chatRoom.Leave("bye");
    }

    public void ShowLobbyMessage(string message) {
      var tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
      scroller.ScrollToBottom();
    }

    private void SendMessage() {
      if (string.IsNullOrWhiteSpace(input.Text)) return;
      chatRoom.PublicMessage(input.Text);
      ShowMessage(Client.LoginPacket.AllSummonerData.Summoner.Name, input.Text);
      input.Text = "";
    }

    private void ShowMessage(string user, string message) {
      var tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
      tr.Text = user + ": ";
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.CornflowerBlue);
      tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, App.FontBrush);
      scroller.ScrollToBottom();
    }

    private void TextBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter) SendMessage();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      SendMessage();
    }
  }
}
