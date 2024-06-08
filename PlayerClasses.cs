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
        public int currentMana { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Charisma { get; set; }


        public int Level { get; set; }
        public int currentExp { get; set; }
        public int maxExp { get; set; }
        public Point playerPos;

        [XmlIgnore] public Inventory inventory { get; set; } = new Inventory();

        [XmlIgnore] public Equipment equipment { get; set; } = new Equipment();

        public Player()
        {
            maxExp = 10;
            Level = 1;
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

        public void ReceiveAttack(int damage)
        {
            currentHealth -= damage;
            if (currentHealth < 0)
            {
                currentHealth = 0;
                PlayerDies();
            }
        }

        public void PlayerDies()
        {
            Console.WriteLine("You have died. Game over.");
            Environment.Exit(0);
        }

        public async Task initialiseInventory()
        {
            await inventory.updateInventoryJSON();
        }
        
        public async Task initialiseEquipment()
        {
            await equipment.updateEquipmentJSON();
        }

        public async Task updatePlayerStatsXML()
        {
            string path = UtilityFunctions.saveFile;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error writing to XML file in updatePlayerStatsXML: {e}");
            }
        }
        
        public void updatePlayerStatsXMLSync()
        {
            string path = UtilityFunctions.saveFile;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Player));
                using (StreamWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error writing to XML file in updatePlayerStatsXML: {e}");
            }
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
            Player tempPlayer = await gameSetup.generateMainXml(chat, prompt5, this);
            //Console.WriteLine($"TempPlayer stat charisma: {tempPlayer.Charisma}");
            
            // assign this player to tempPlayer
            PropertyInfo[] properties = typeof(Player).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    object value = property.GetValue(tempPlayer);
                    PropertyInfo thisProperty = this.GetType().GetProperty(property.Name);

                    if (thisProperty != null && thisProperty.CanWrite)
                    {
                        thisProperty.SetValue(this, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property in initialisePlayerFromNarrator {property.Name}: {ex.Message}");
                }
            }

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


        public void checkForLevelUp()
        {
            /*while (currentExp >= maxExp)
            {
                levelUp();
            }*/
        }
    }
}