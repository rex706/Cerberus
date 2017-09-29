using System.Threading.Tasks;
using PUBGSharp;
using PUBGSharp.Data;
using PUBGSharp.Helpers;
using PUBGSharp.Net.Model;
using System;

namespace Cerberus_GUI2
{
    class PUBGTracker
    {
        // Input Player Name and Game Mode to retrieve rank.
        // 0 = solos, 1 = duos, 2 = squads
        public static async Task<int> getRankAsync(string name, int mode)
        {
            PUBGStatsClient statsClient = null;
            StatsResponse stats = null;

            try
            {
                statsClient = new PUBGStatsClient("a9560bcb-a1ad-4360-9b77-8a294b40d97d");
                stats = await statsClient.GetPlayerStatsAsync(name);
            }
            catch (Exception m)
            {
                Console.WriteLine(m.Message);
            }

            if (stats == null)
                return -1;
            else if (stats.Stats.Count == 0)
                return -2;
                
            int rank = -1;

            try
            {
               rank = stats.Stats.FindLast(x => x.Mode == (Mode)mode).Stats.Find(x => x.Stat == Stats.Rating).Rank.Value;
            }
            catch (Exception m)
            {
                Console.WriteLine(m.Message);
            }

            if (rank == -1)
                return -3;
            else
                return rank;
        }
    }
}
