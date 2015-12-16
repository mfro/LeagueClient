using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;
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

namespace LeagueClient.Logic.Chat {
  public class ChatRoomController {
    private TextBox input;
    private RichTextBox output;
    private Button send;
    private ScrollViewer scroller;

    private Jid chatRoom;
    private Dispatcher dispatch = Application.Current.Dispatcher;

    public bool IsJoined { get; private set; }
    public Dictionary<string, LeagueStatus> Statuses { get; } = new Dictionary<string, LeagueStatus>();
    public Dictionary<string, Item> Users { get; } = new Dictionary<string, Item>();

    public ChatRoomController(TextBox input, RichTextBox output, Button send, ScrollViewer scroller) {
      this.input = input;
      this.output = output;
      this.send = send;
      this.scroller = scroller;

      input.KeyUp += TextBox_KeyUp;
      send.Click += Button_Click;
    }

    public void JoinChat(Jid jid, string pass) {
      if (IsJoined) return;
      chatRoom = jid;
      Client.ChatManager.JoinRoom(jid, pass);
      Client.ChatManager.PresenceRecieved += ChatManager_PresenceRecieved;
      Client.ChatManager.MessageReceived += ChatManager_MessageReceived;
      IsJoined = true;
    }

    private void ChatManager_MessageReceived(object sender, Message e) {
      if (!e.From.User.Equals(chatRoom.User) || e.Type != MessageType.groupchat) return;

      Application.Current.Dispatcher.MyInvoke(ShowChatMessage, e.From.Resource, e.Body);
    }

    private void ChatManager_PresenceRecieved(object sender, Presence e) {
      if (!e.From.User.Equals(chatRoom.User)) return;
      if (e.Status == null && e.Type == PresenceType.available) {
        Client.ChatManager.SendPresence();
        return;
      }

      var user = e.MucUser.Item;
      if (e.Type == PresenceType.available) {
        if (!Users.ContainsKey(user.Jid.User)) Application.Current.Dispatcher.MyInvoke(ShowLobbyMessage, $"{e.From.Resource} has joined the lobby");
        Statuses[user.Jid.User] = new LeagueStatus(e.Status, e.Show);
        Users[user.Jid.User] = user;
      } else {
        if (Users.ContainsKey(user.Jid.User)) Application.Current.Dispatcher.MyInvoke(ShowLobbyMessage, $"{e.From.Resource} has left the lobby");
        Statuses.Remove(user.Jid.User);
        Users.Remove(user.Jid.User);
      }
    }

    public void LeaveChat() {
      Client.ChatManager.LeaveRoom(chatRoom);
      Client.ChatManager.PresenceRecieved -= ChatManager_PresenceRecieved;
      Client.ChatManager.MessageReceived -= ChatManager_MessageReceived;
    }

    public void ShowLobbyMessage(string message) {
      var tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
      if (scroller.VerticalOffset == scroller.ScrollableHeight)
        scroller.ScrollToBottom();
    }

    private void SendMessage() {
      if (string.IsNullOrWhiteSpace(input.Text)) return;
      Client.ChatManager.SendMessage(chatRoom, input.Text, MessageType.groupchat);
      input.Text = "";
    }

    private void ShowChatMessage(string user, string message) {
      var tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
      tr.Text = user + ": ";
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.CornflowerBlue);
      tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
      tr.Text = message + '\n';
      tr.ApplyPropertyValue(TextElement.ForegroundProperty, App.FontBrush);
      if (scroller.VerticalOffset == scroller.ScrollableHeight)
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
