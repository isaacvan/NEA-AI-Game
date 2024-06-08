using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EnemyClassesNamespace;

namespace CombatNamespace
{
    public class Combat
    {
        public Player player { get; set; }
        public Enemy enemy { get; set; }
        public List<Status> statuses { get; set; }
        // more

        public static void createStatus(string name, string effect) // DYNAMICEXPRESSO ??
        {
            // create a new status and add it to statuses
        }
    }

    public class Status
    {
        public string name { get; set; }
        public string effect { get; set; }
        
    }
}