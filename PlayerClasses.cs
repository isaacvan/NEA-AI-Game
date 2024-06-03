using EnemyClassesNamespace;
using UtilityFunctionsNamespace;
using GPTControlNamespace;
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
using ItemFunctionsNamespace;
using OpenAI_API;
using OpenAI_API.Chat;

namespace PlayerClassesNamespace
{
    [Serializable]
    public class Player
    {
        public string Class { get; set; }
        public string Race { get; set; }
        public int Health { get; set; }
        public int currentHealth { get; set; }
        public int ManaPoints { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }


        public int level { get; set; }
        public int currentExp { get; set; }
        public int maxExp { get; set; }
        public Point playerPos;
        public Inventory inventory { get; set; }
        public Equipment equipment { get; set; }

        public Player()
        {
            maxExp = 10;
            level = 1;
            currentExp = 0;
            playerPos = new Point(0, 0);
        }
        
        public void EquipItem(EquippableItem.EquipLocation slot, Item item)
        {
            equipment.EquipItem(slot, item, inventory);
        }

        public void UnequipItem(EquippableItem.EquipLocation slot)
        {
            equipment.UnequipItem(slot, inventory);
        }

        public void AddItem(Item item)
        {
            inventory.AddItem(item);
        }
        
        public void RemoveItem(Item item)
        {
            inventory.RemoveItem(item);
        }

        public async Task initialiseInventory()
        {
            inventory = new Inventory();
            await inventory.updateInventoryJSON();
        }
        
        public async Task initialiseEquipment()
        {
            equipment = new Equipment();
            await equipment.updateEquipmentJSON();
        }

        public async Task initialisePlayerFromNarrator(GameSetup gameSetup, OpenAIAPI api, Conversation chat, bool testing = false)
        {
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Creating Character...", UtilityFunctions.typeSpeed);


            // load prompt 5
            string prompt5 = "";
            Console.ForegroundColor = ConsoleColor.White;
            try
            {
                prompt5 = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt5.txt");
            }
            catch (Exception e)
            {
                throw new Exception($"Could not find prompt file: {e}");
            }

            // get response from GPT
            string output = "";
            await gameSetup.generateMainXml(chat, prompt5);


            // set character health to max
            currentHealth = Health;
            // this.
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Character Created!", UtilityFunctions.typeSpeed);
            Console.ForegroundColor = ConsoleColor.White;
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
            /*while (currentExp >= maxExp)
            {
                levelUp();
            }*/
        }
    }
}