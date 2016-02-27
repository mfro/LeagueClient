using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiotClient.Lobbies {
  public abstract class Lobby : IDisposable {
    public event EventHandler<MemberEventArgs> MemberJoined;
    public event EventHandler<MemberEventArgs> MemberLeft;

    public event EventHandler LeftLobby;
    public event EventHandler Loaded;

    public abstract GroupChat ChatLobby { get; protected set; }

    protected bool loaded;

    public abstract void Dispose();

    internal abstract bool HandleMessage(MessageReceivedEventArgs args);

    protected virtual void OnMemberJoined(LobbyMember member) {
      MemberJoined?.Invoke(this, new MemberEventArgs(member));
    }

    protected virtual void OnMemberLeft(LobbyMember member) {
      MemberLeft?.Invoke(this, new MemberEventArgs(member));
    }

    protected virtual void OnLeftLobby() {
      LeftLobby?.Invoke(this, new EventArgs());
    }

    protected virtual void OnLoaded() {
      if (loaded) throw new NotImplementedException("Called onload twice");
      loaded = true;
      Loaded?.Invoke(this, new EventArgs());
    }
  }

  public abstract class LobbyMember {
    public event EventHandler Changed;

    protected Lobby lobby;

    public abstract string Name { get; }
    public abstract long SummonerID { get; }
    public bool IsMe => SummonerID == Session.Current.Account.SummonerID;

    public LobbyMember(Lobby lobby) {
      this.lobby = lobby;
    }

    protected void OnChange() {
      Changed?.Invoke(this, new EventArgs());
    }
  }

  public class MemberEventArgs : EventArgs {
    public LobbyMember Member { get; }
    public MemberEventArgs(LobbyMember member) {
      Member = member;
    }
  }
}
