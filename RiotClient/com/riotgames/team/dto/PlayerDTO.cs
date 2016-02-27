using System;
using RtmpSharp.IO;
using System.Collections.Generic;

namespace RiotClient.Riot.Team
{
    [Serializable]
    [SerializedName("com.riotgames.team.dto.PlayerDTO")]
    public class PlayerDTO
    {
        [SerializedName("playerId")]
        public Double PlayerId { get; set; }

        [SerializedName("teamsSummary")]
        public List<TeamDTO> TeamsSummary { get; set; }

        [SerializedName("createdTeams")]
        public List<CreatedTeam> CreatedTeams { get; set; }

        [SerializedName("playerTeams")]
        public List<TeamInfo> PlayerTeams { get; set; }
    }
}
