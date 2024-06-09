using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using EnemyClassesNamespace;

namespace CombatNamespace
{
    public class Combat
    {
        public Player player { get; set; }
        public Enemy enemy { get; set; }
        // more
    }

    public class StatusFactory
    {
        public List<Status> statusList { get; set; } = new List<Status>();
        
        public static Status CreateStatus(string name, string duration, string type, bool increase, bool percentBool, int? intensityNumber, int? intensityPercent, bool stackable, bool refreshable, string description) 
        {
            if (percentBool)
            {
                if (intensityPercent == null) {
                    throw new Exception("Intensity percent cannot be null if percent bool is true.");
                }
                else
                {
                    intensityNumber = null;
                    intensityPercent = (int)(intensityPercent / 100);
                }
            }
            else
            {
                if (intensityNumber == null)
                {
                    throw new Exception("Intensity number cannot be null if percent bool is false.");
                }
                else
                {
                    intensityPercent = null;
                    intensityNumber = (int)intensityNumber;
                }
            }
            
            return new Status
            {
                Name = name, 
                Duration = int.Parse(duration),
                Type = type,
                PercentBool = percentBool,
                Increase = increase,
                IntensityNumber = intensityNumber,
                IntensityPercent = intensityPercent,
                Stackable = stackable,
                Refreshable = refreshable,
                Description = description
            };
        }
    }

    public class Status
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public string Type { get; set; }
        public bool Increase { get; set; }
        public bool PercentBool { get; set; }
        public int? IntensityNumber { get; set; }
        public int? IntensityPercent { get; set; }
        public bool Stackable { get; set; }
        public bool Refreshable { get; set; }
        public string Description { get; set; }
    }
}