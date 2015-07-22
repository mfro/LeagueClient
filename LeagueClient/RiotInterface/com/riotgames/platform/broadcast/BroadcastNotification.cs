using System;
using RtmpSharp.IO;
using RtmpSharp.IO.AMF3;
using System.Text;
using System.Collections.Generic;
using System.Collections;

namespace LeagueClient.RiotInterface.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.broadcast.BroadcastNotification")]
  public class BroadcastNotification : IExternalizable {
    public ArrayList broadcastMessages { get; set; }

    public string Json { get; set; }

    public void ReadExternal(IDataInput input) {
      Json = input.ReadUtf((int) input.ReadUInt32());

      var json = MFroehlich.Parsing.DynamicJSON.JSON.ParseObject(Json);

      Type classType = typeof(BroadcastNotification);
      foreach (KeyValuePair<string, object> keyPair in json) {
        var f = classType.GetProperty(keyPair.Key);
        f.SetValue(this, keyPair.Value);
      }
    }

    public void WriteExternal(IDataOutput output) {
      var bytes = Encoding.UTF8.GetBytes(Json);

      output.WriteInt32(bytes.Length);
      output.WriteBytes(bytes);
    }
  }
}
