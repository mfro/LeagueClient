using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.x.muc;
using RiotClient.Lobbies;
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
  public sealed class ChatRoom {
    private TextBox input;
    private RichTextBox output;
    private Button send;
    private ScrollViewer scroller;

    private GroupChat lobby;
    private Dispatcher dispatch = Application.Current.Dispatcher;

    public ChatRoom(GroupChat lobby, TextBox input, RichTextBox output, Button send, ScrollViewer scroller) {
      this.input = input;
      this.output = output;
      this.send = send;
      this.scroller = scroller;
      this.lobby = lobby;

      input.KeyUp += TextBox_KeyUp;
      send.Click += Button_Click;

      lobby.ChatMessage += Lobby_ChatMessage;
      lobby.ChatStatus += Lobby_ChatStatus;
    }

    private void Lobby_ChatMessage(object sender, ChatMessageEventArgs e) {
      dispatch.Invoke(() => {
        var tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
        tr.Text = e.From + ": ";
        tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.CornflowerBlue);
        tr = new TextRange(output.Document.ContentEnd, output.Document.ContentEnd);
        tr.Text = e.Message + '\n';
        tr.ApplyPropertyValue(TextElement.ForegroundProperty, App.FontBrush);
        if (scroller.VerticalOffset == scroller.ScrollableHeight)
          scroller.ScrollToBottom();
      });
    }

    private void Lobby_ChatStatus(object sender, ChatStatusEventArgs e) {
      if (e.IsOnline) {
        dispatch.MyInvoke(ShowLobbyMessage, $"{e.From} has joined the lobby");
      } else {
        dispatch.MyInvoke(ShowLobbyMessage, $"{e.From} has left the lobby");
      }
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
      lobby.SendMessage(input.Text);
      input.Text = "";
    }

    private void TextBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter) SendMessage();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      SendMessage();
    }
  }
}
