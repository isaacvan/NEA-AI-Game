using EnemyClassesNamespace;
using GridConfigurationNamespace;
using MainNamespace;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;

namespace UIGenerationNamespace
{
    public class UIConstructer
    {
        public Player player { get; set; }
        public List<Enemy> nearbyEnemies { get; set; }
        public List<List<Tile>> map { get; set; }

        public UIConstructer(Player plyr)
        {
            player = plyr;
        }

        public async Task fillNearbyEnemies() // needs filling
        {
            foreach (List<Tile> row in map)
            {
                foreach (Tile tile in row)
                {
                    /* if (tile.enemyHere)
                    {
                     nearbyEnemies.Add(tile.getEnemy())
                    }*/
                }
            }
        }

        public async Task drawUI()
        {
            // main UI fnction
        }
        
        public void displayCombatUI()
        {
            // combat ui
            
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("[ Player ]");
            Console.WriteLine($"Class: {player.Class}");
            Console.WriteLine($"Level: {player.Level}");
            Console.WriteLine($"HP: [{UtilityFunctions.DrawHealthBar(player)}\x1b[0m]");
            Console.WriteLine($"Statuses: {String.Join(", ", player.statusMap.Values.Select(status => status))}");
            Console.WriteLine("----------------------------------------");
        }
    }
}