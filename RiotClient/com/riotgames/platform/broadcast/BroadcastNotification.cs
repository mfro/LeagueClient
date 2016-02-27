using System;
using RtmpSharp.IO;
using RtmpSharp.IO.AMF3;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using MFroehlich.Parsing.JSON;

namespace RiotClient.Riot.Platform {
  [Serializable]
  [SerializedName("com.riotgames.platform.broadcast.BroadcastNotification")]
  public class BroadcastNotification : IExternalizable {
    public List<BroadcastMessage> BroadcastMessages { get; set; }

    public string Json { get; set; }

    public void ReadExternal(IDataInput input) {
      Json = input.ReadUtf((int) input.ReadUInt32());

      var json = JSONParser.ParseObject(Json, 0);

      if (json.ContainsKey("broadcastMessages")) {

        BroadcastMessages = JSONDeserializer.Deserialize<List<BroadcastMessage>>(json["broadcastMessages"]);
      }
    }

    public void WriteExternal(IDataOutput output) {
      var bytes = Encoding.UTF8.GetBytes(Json);

      output.WriteInt32(bytes.Length);
      output.WriteBytes(bytes);
    }
  }
}
