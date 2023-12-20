using EnemyClassesNamespace;
using UtilityFunctionsNamespace;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Security.Cryptography;
using System.Dynamic;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace PlayerClassesNamespace
{


    [Serializable]
    [XmlInclude(typeof(Warrior))]
    [XmlInclude(typeof(Mage))]
    [XmlInclude(typeof(Rogue))]
    public abstract class Player
    {
        public int strength { get; set; }
        public int dexterity { get; set; }
        public int intelligence { get; set; }
        public int maxHealth { get; set; }
        public int currentHealth { get; set; }
        public int defense { get; set; }
        public int dodge { get; set; }
        public int level { get; set; }
        public int currentExp { get; set; }
        public int maxExp { get; set; }
        public List<int> stats { get; set; }
        public int madnessTurns { get; set; }
        public Point playerPos;
        public string[][] inventory;
        public int typeSpeed = 1;


        public Player()
        {
            maxExp = 15;
        }

        public void changePlayerPos(Point newPos)
        {
            playerPos = newPos;
        }

        public void changePlayerStats(string stat, int newValue)
        {
            string fileName = UtilityFunctions.saveFile;

            // Load the XML document
            XDocument document = XDocument.Load(fileName);

            // Locate the element in the XML document
            XElement statElement = document.Element("Player")?.Element(stat);

            if (statElement != null)
            {
                // Update player stat
                statElement.SetValue((object)newValue);
                document.Save(fileName);
            }
            else
            {
                Console.WriteLine($"Element {stat} not found in {fileName}.");
            }
        }


        public void checkForLevelUp()
        {

        }
    }
    public class Mage : Player
    {
        public Mage()
        {
            string[] lines = File.ReadAllLines(UtilityFunctions.mainDirectory + @"Classes\Mage.txt");
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                string statName = parts[0].Trim();
                int statValue = int.Parse(parts[1].Trim());

                switch (statName)
                {
                    case "strength":

                        strength = statValue;
                        break;
                    case "dexterity":


                        dexterity = statValue;
                        break;
                    case "intelligence":


                        intelligence = statValue;
                        break;
                    case "currentHealth":


                        currentHealth = statValue;
                        break;
                    case "maxHealth":


                        maxHealth = statValue;
                        break;
                    case "defense":


                        defense = statValue;
                        break;
                    case "dodge":


                        dodge = statValue;
                        break;
                    default:
                        Console.WriteLine("error");
                        break;
                }
            }
        }
    }










    public class Rogue : Player
    {
        public Rogue()
        {
            string[] lines = File.ReadAllLines(UtilityFunctions.mainDirectory + @"Classes\Rogue.txt");
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                string statName = parts[0].Trim();
                int statValue = int.Parse(parts[1].Trim());

                if (statName == "strength")
                {
                    strength = statValue;
                }
                else if (statName == "dexterity")
                {
                    dexterity = statValue;
                }
                else if (statName == "intelligence")
                {
                    intelligence = statValue;
                }
                else if (statName == "currentHealth")
                {
                    currentHealth = statValue;
                }
                else if (statName == "maxHealth")
                {
                    maxHealth = statValue;
                }
                else if (statName == "defense")
                {
                    defense = statValue;
                }
                else if (statName == "dodge")
                {
                    dodge = statValue;
                }
            }
        }
    }






    public class Warrior : Player
    {
        public Warrior()
        {
            string[] lines = File.ReadAllLines(UtilityFunctions.mainDirectory + @"Classes\Warrior.txt");
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                string statName = parts[0].Trim();
                int statValue = int.Parse(parts[1].Trim());

                if (statName == "strength")
                {
                    strength = statValue;
                }
                else if (statName == "dexterity")
                {
                    dexterity = statValue;
                }
                else if (statName == "intelligence")
                {
                    intelligence = statValue;
                }
                else if (statName == "currentHealth")
                {
                    currentHealth = statValue;
                }
                else if (statName == "maxHealth")
                {
                    maxHealth = statValue;
                }
                else if (statName == "defense")
                {
                    defense = statValue;
                }
                else if (statName == "dodge")
                {
                    dodge = statValue;
                }
            }
        }
    }
}