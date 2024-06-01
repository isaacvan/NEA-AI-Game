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
using System.Reflection;

namespace PlayerClassesNamespace
{


    [Serializable]
    public class Player
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
            maxExp = 10;
            level = 1;
        }

        public void changePlayerPos(Point newPos)
        {
            playerPos = newPos;
        }

        public void changePlayerStats(string stat, int newValue, bool midLevelUp = false)
        {
            GetType().GetProperty(stat).SetValue(this, newValue);

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
            if (!midLevelUp)
            {
                checkForLevelUp();
            }

        }


        public void checkForLevelUp()
        {
            while (currentExp >= maxExp)
            {
                levelUp();
            }
        }


        public void levelUp()
        {
            int newMaxExp = maxExp + (level * 10);
            int newCurrentExp = currentExp - maxExp;
            int newMaxHealth = maxHealth + (level * 10);
            int newLevel = level + 1;
            int newStrength = strength + (level * 2);
            int newDexterity = dexterity + (level * 2);
            int newIntelligence = intelligence + (level * 2);
            int newDefense = defense + (level * 2);
            int newDodge = dodge + (level * 2);
            Console.Clear();
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"You levelled up from level {UtilityFunctions.colourScheme.generalAccentCode}{level}{UtilityFunctions.colourScheme.generalTextCode} to level {UtilityFunctions.colourScheme.generalAccentCode}{newLevel}{UtilityFunctions.colourScheme.generalTextCode}!", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Max HP: {UtilityFunctions.colourScheme.generalAccentCode}{maxHealth}{UtilityFunctions.colourScheme.generalTextCode} => {UtilityFunctions.colourScheme.generalAccentCode}{newMaxHealth}", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Strength: {UtilityFunctions.colourScheme.generalAccentCode}{strength}{UtilityFunctions.colourScheme.generalTextCode} => {UtilityFunctions.colourScheme.generalAccentCode}{newStrength}", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Dexterity: {UtilityFunctions.colourScheme.generalAccentCode}{dexterity}{UtilityFunctions.colourScheme.generalTextCode} => {UtilityFunctions.colourScheme.generalAccentCode}{newDexterity}", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Intelligence: {UtilityFunctions.colourScheme.generalAccentCode}{intelligence}{UtilityFunctions.colourScheme.generalTextCode} => {UtilityFunctions.colourScheme.generalAccentCode}{newIntelligence}", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Defense: {UtilityFunctions.colourScheme.generalAccentCode}{defense}{UtilityFunctions.colourScheme.generalTextCode} => {UtilityFunctions.colourScheme.generalAccentCode}{newDefense}", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"Dodge: {UtilityFunctions.colourScheme.generalAccentCode}{dodge}{UtilityFunctions.colourScheme.generalTextCode} => {UtilityFunctions.colourScheme.generalAccentCode}{newDodge}", UtilityFunctions.typeSpeed);
            changePlayerStats("maxHealth", newMaxHealth, true);
            changePlayerStats("currentHealth", newMaxHealth, true);
            changePlayerStats("level", newLevel, true);
            changePlayerStats("maxExp", newMaxExp, true);
            changePlayerStats("currentExp", newCurrentExp, true);
            changePlayerStats("strength", newStrength, true);
            changePlayerStats("dexterity", newDexterity, true);
            changePlayerStats("intelligence", newIntelligence, true);
            changePlayerStats("defense", newDefense, true);
            changePlayerStats("dodge", newDodge, true);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, $"\n\nPress any key to leave:", UtilityFunctions.typeSpeed);
            Console.ReadLine();
            UtilityFunctions.clearScreen(this);
        }

    }
}