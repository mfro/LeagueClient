using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RtmpSharp.IO;

namespace RiotClient.com.riotgames.platform.harassment {

  [Serializable]
  [SerializedName("com.riotgames.platform.harassment.LcdsResponseString")]
  public class HarassmentReport {

    [SerializedName("offense")]
    public String Offense { get; set; }

    [SerializedName("gameId")]
    public String GameId { get; set; }

    [SerializedName("reportSource")]
    public String ReportSource { get; set; }

    [SerializedName("reportingSummonerId")]
    public String ReportingSummonerId { get; set; }

    [SerializedName("reportedSummonerId")]
    public String ReportedSummonerId { get; set; }

    [SerializedName("comment")]
    public String Comment { get; set; }
  }
}
