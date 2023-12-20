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
        public List<string> statLabels = new List<string> { "strength", "dexterity", "intelligence", "currentHealth", "maxHealth", "defense", "dodge", "currentExp", "maxExp" };
        public int madnessTurns { get; set; }
        public Point playerPos;
        public string[][] inventory;
        public abstract void PerformBasicAttack(Enemy enemy);
        public abstract void ClassAttack(Enemy enemy);
        public abstract bool RunAway(int round);
        public int typeSpeed = 1;


        public Player()
        {
        }

        public void changePlayerPos(Point newPos)
        {
            playerPos = newPos;
        }

        public void changePlayerStats(string stat, int newValue)
        {
            
            XDocument document = XDocument.Load(UtilityFunctions.saveFile);

            // Locate the save slot in the XML document
            XElement saveElement = document.Element("Saves")?.Element(UtilityFunctions.saveSlot);
            if (saveElement != null)
            {
                // Update player stats
                saveElement.Element(stat)?.SetValue(newValue);
            }

            // Save the updated document
            document.Save("saves.xml");
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

            stats = new List<int> { strength, dexterity, intelligence, currentHealth, maxHealth, defense, dodge, currentExp, maxExp };
            foreach (string statLabel in statLabels)
            {
                UtilityFunctions.addInfoToSaveFile(statLabel, stats[statLabels.IndexOf(statLabel)].ToString());
            }
        }



        public override void PerformBasicAttack(Enemy enemy)
        {

            // Calculate damage based on Mage's unique move and stats

            float damage = (float)intelligence;
            damage *= (float)1.2;
            enemy.currentHealth -= (int)damage * enemy.getDefensePerc();
            if (enemy.currentHealth <= 0)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou fire a powerful energy orb, slaying the " + enemy.type + ".", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou fire a powerful energy orb!", typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", typeSpeed);
            }
        }

        public override bool RunAway(int round)
        {
            float escapeChance = (float)(dodge);
            escapeChance += (float)(dexterity * 0.2);
            escapeChance += (float)round * 8;
            Random random = new Random();
            float randomNumber = (float)random.NextDouble();
            bool escaped = false;
            if (randomNumber <= escapeChance)
            {
                escaped = true;
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "You escaped!", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Escape attempt failed", typeSpeed);
            }
            return escaped;
        }

        public override void ClassAttack(Enemy enemy)
        {
            ApplyBurning(enemy);

            float damage = (float)intelligence * 1.2f;
            enemy.currentHealth -= (int)damage * enemy.getDefensePerc();



            if (enemy.currentHealth <= 0)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou shoot a \x1b[38;2;255;140;0mfireball\x1b[0m at the " + enemy.type + " with a fatal blow.", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou shoot a \x1b[38;2;255;140;0mfireball\x1b[0m at the " + enemy.type + ", causing them to \x1b[38;2;255;140;0mburn\x1b[0m.", typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", typeSpeed);
            }
        }

        public void ApplyBurning(Enemy enemy)
        {
            enemy.burningTurns += 2;
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

            stats = new List<int> { strength, dexterity, intelligence, currentHealth, maxHealth, defense, dodge, currentExp, maxExp };
            foreach (string statLabel in statLabels)
            {
                UtilityFunctions.addInfoToSaveFile(statLabel, stats[statLabels.IndexOf(statLabel)].ToString());
            }
        }



        public override void PerformBasicAttack(Enemy enemy)
        {

            // Calculate damage based on Rogue's unique move and stats

            float damage = (float)dexterity;
            damage *= (float)1.2;
            enemy.currentHealth -= (int)damage * enemy.getDefensePerc();
            if (enemy.currentHealth <= 0)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou stab the enemy relentlessly, slaying the " + enemy.type + ".", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou stab the enemy relentlessly!", typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", typeSpeed);
            }
        }

        public override bool RunAway(int round)
        {
            float escapeChance = (float)(dodge);
            escapeChance += (float)(dexterity * 0.2);
            escapeChance += (float)round * 8;
            Random random = new Random();
            float randomNumber = (float)random.NextDouble();
            bool escaped = false;
            if (randomNumber <= escapeChance)
            {
                escaped = true;
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "You escaped!", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Escape attempt failed", typeSpeed);
            }
            return escaped;
        }

        public static void ApplyBleed(Enemy enemy)
        {
            int bleedDuration = 3; // Set the duration of the bleed effect
            enemy.Bleed += bleedDuration;
        }

        public override void ClassAttack(Enemy enemy)
        {

            float damage = (float)dexterity;
            damage *= (float)1;
            int temp = enemy.Bleed - 3;
            if (temp < 0)
            {
                enemy.roundBonus = 0;
            }
            else
            {
                enemy.roundBonus = temp;
            }
            ApplyBleed(enemy);
            enemy.currentHealth -= (int)damage * enemy.getDefensePerc();
            if (enemy.currentHealth <= 0)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou backstab the " + enemy.type + " with a fatal blow.", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou backstab the " + enemy.type + ", causing him to \x1b[31mbleed\x1b[0m relentlessly.", typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", typeSpeed);
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

            stats = new List<int> { strength, dexterity, intelligence, currentHealth, maxHealth, defense, dodge, currentExp, maxExp };
            foreach (string statLabel in statLabels)
            {
                UtilityFunctions.addInfoToSaveFile(statLabel, stats[statLabels.IndexOf(statLabel)].ToString());
            }
        }



        public override void PerformBasicAttack(Enemy enemy)
        {

            // Calculate damage based on Warrior's unique move and stats

            float damage = (float)strength;
            damage *= (float)1.2;
            enemy.currentHealth -= (int)damage * enemy.getDefensePerc();
            if (enemy.currentHealth <= 0)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou slash at the enemy wildy, slaying the " + enemy.type + ".", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou slash at the enemy wildy!", typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", typeSpeed);
            }

        }

        public override bool RunAway(int round)
        {
            float escapeChance = (float)(dodge);
            escapeChance += (float)(dexterity * 0.2);
            escapeChance += (float)round * 8;
            Random random = new Random();
            float randomNumber = (float)random.NextDouble();
            bool escaped = false;
            if (randomNumber <= escapeChance)
            {
                escaped = true;
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "You escaped!", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "Escape attempt failed", typeSpeed);
            }
            return escaped;
        }

        public override void ClassAttack(Enemy enemy)
        {
            ApplyMadness(); // stacks and increases warriors damage but decreases warriors health / def  

            float damage = (float)strength * 1.2f;
            damage *= (float)madnessTurns;
            enemy.currentHealth -= (int)damage * enemy.getDefensePerc();

            if (enemy.currentHealth <= 0)
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nWith \x1b[38;2;255;140;0mcrazed eyes\x1b[0m and a mighty axe, you deliver a fatal blow to the " + enemy.type + ".", typeSpeed);
            }
            else
            {
                UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nYou swing your axe with \x1b[38;2;255;140;0mberserker rage\x1b[0m, striking the " + enemy.type + ".", typeSpeed);
                UtilityFunctions.TypeText(UtilityFunctions.Instant, $"The {enemy.type} now has \n\x1b[31m{enemy.currentHealth} / {enemy.maxHealth} health\x1b[0m left.", typeSpeed);
            }
        }

        public void ApplyMadness()
        {
            madnessTurns += 2;
        }
    }
}