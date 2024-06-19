using EnemyClassesNamespace;
using GridConfigurationNamespace;
using PlayerClassesNamespace;

namespace UIGenerationNamespace
{
    public class UIConstructer
    {
        public Player player { get; set; }
        public List<Enemy> nearbyEnemies { get; set; }
        public List<List<Tile>> map { get; set; }

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
        }
    }
}