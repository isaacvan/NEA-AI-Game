using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/**
 * Docs here
 */
namespace EnemyClassesNamespace
{
    public abstract class Enemy
    {
        public int strength { get; set; }
        public int dexterity { get; set; }
        public int intelligence { get; set; }
        public int maxHealth { get; set; }
        public int currentHealth { get; set; }
        public int defense { get; set; }
        public int dodge { get; set; }
        public int level { get; set; }
        public int expAwarded { get; set; }
        public string type;
        public int typeSpeed = 1;
        public int Bleed { get; set; }
        public int burningTurns { get; set; }
        public int roundBonus { get; set; }
        public abstract void PerformBasicAttack(Player player);
        public abstract int getDefensePerc();

        public Enemy()
        {
            expAwarded = 5;
        }
    }

    class Orc : Enemy
    {
        public Orc()
        {
            string[] lines = File.ReadAllLines(UtilityFunctions.mainDirectory + @"Enemies\Orc.txt");
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

        public override void PerformBasicAttack(Player player)
        {

            // Calculate damage based on Orc's unique move and stats

            float damage = (float)strength;
            damage += (float)dexterity;
            damage *= (float)1;
            player.currentHealth -= (int)damage;
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nOrc performs a powerful strike!\nYou have \x1b[31m" + player.currentHealth + "hp remaining.\x1b[0m\n", typeSpeed);
        }

        public override int getDefensePerc()
        {
            int temp = 1 - (defense / 100);
            return temp;
        }
    }

    class Snake : Enemy
    {
        public Snake()
        {
            string[] lines = File.ReadAllLines(UtilityFunctions.mainDirectory + @"Enemies\Snake.txt");
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

        public override void PerformBasicAttack(Player player)
        {

            // Calculate damage based on Snake's unique move and stats

            float damage = (float)dexterity;
            damage += (float)strength;
            damage *= (float)1;
            player.currentHealth -= (int)damage;
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nSnake performs a powerful strike!\nYou have \x1b[31m" + player.currentHealth + "hp remaining.\x1b[0m\n", typeSpeed);
        }

        public override int getDefensePerc()
        {
            int temp = 1 - (defense / 100);
            return temp;
        }
    }


    class Boar : Enemy
    {
        public Boar()
        {
            string[] lines = File.ReadAllLines(UtilityFunctions.mainDirectory + @"Enemies\Boar.txt");
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

        public override void PerformBasicAttack(Player player)
        {

            // Calculate damage based on Boar's unique move and stats

            float damage = (float)strength;
            damage += (float)dexterity;
            damage *= (float)1;
            player.currentHealth -= (int)damage;
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "\nBoar performs a powerful strike!\nYou have \x1b[31m" + player.currentHealth + "hp remaining.\x1b[0m\n", typeSpeed);
        }

        public override int getDefensePerc()
        {
            int temp = 1 - (defense / 100);
            return temp;
        }
    }
}