using System;
using System.IO;
using OpenAI_API;
using OpenAI_API.Chat;
using UtilityFunctionsNamespace;

namespace ItemFunctionsNamespace
{
    public class Inventory
    {
        public List<Item> Items { get; set; }
        public List<int> Quantaties { get; set; }

        public static void AddItem(Item item)
        {
            
        }

        public static void RemoveItem(Item item)
        {

        }

        public static List<Item> ListItems(Inventory inventory)
        {
            return new List<Item>();
        }
    }
    
    public class ItemFactory
    {
        public List<ItemTemplate> templates { get; set; }

        public ItemFactory(OpenAIAPI api, Conversation chat)
        {
            
        }

        public static async Task initialiseItemFactoryFromNarrator(OpenAIAPI api, Conversation chat)
        {
            Console.WriteLine("Initialising item factory from narrator...");
            
            // initialise path
            UtilityFunctions.itemTemplateSpecificDirectory =
                UtilityFunctions.itemTemplateDir + UtilityFunctions.saveName;
            
            // load item templates from narrator
            string prompt6 = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt6.txt");
            
            // get response from GPT
            string output = "";
            try
            {
                // output = await Narrator.getGPTResponse(prompt5, api, 100, 0.9);
                chat.AppendUserInput(prompt6);
                output = await chat.GetResponseFromChatbotAsync();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not get response: {e}");
            }

            if (string.IsNullOrEmpty(output.Trim()))
            {
                throw new Exception("No response received from GPT.");
            }
            
            // design xml file
            string preText = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            output = await UtilityFunctions.cleanseXML(output);
            string finalXMLText = "";
            finalXMLText = output;

            List<string> inheritableTraits = new List<string>() { "Weapon", "Consumable", "Armour" };
            
            // split into multiple files in string format
            string[] lines = finalXMLText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            List<string> lineList = lines.ToList();
            List<string> weaponList = new List<string>();
            List<string> consumableList = new List<string>();
            List<string> armourList = new List<string>();
            
            
            int temp = 0;

            
            foreach (string line in lineList)
            {
                if (line.Contains("SEPERATOR", StringComparison.Ordinal))
                {
                    temp++;
                }
                else if (temp < 3)
                {
                    switch (temp)
                    {
                        case 0:
                            weaponList.Add(line);
                            break;
                        case 1:
                            consumableList.Add(line);
                            break;
                        case 2:
                            armourList.Add(line);
                            break;
                    }
                }

                
            }
            
            string weaponXML = string.Join("\n", weaponList);
            weaponXML = preText + "\n" + weaponXML;
            string consumableXML = string.Join("\n", consumableList);
            consumableXML = preText + "\n" + consumableXML;
            string armourXML = string.Join("\n", armourList);
            armourXML = preText + "\n" + armourXML;

            
            int traitIndex = 0;
            List<string> listOfFinalXMLs = new List<string>() { weaponXML, consumableXML, armourXML };
            
            
            
            //Console.WriteLine(UtilityFunctions.itemTemplateSpecificDirectory);
            //Console.ReadLine();
            
            // create directory for this game
            if (!Directory.Exists(UtilityFunctions.itemTemplateSpecificDirectory))
            {
                try
                {
                    Directory.CreateDirectory(UtilityFunctions.itemTemplateSpecificDirectory);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                throw new Exception("Item template directory already exists.");
            }
            
            
            // write to file
            // creating files for each trait
            foreach (string inheritableTrait in inheritableTraits)
            {
                try
                {
                    File.Create($@"{UtilityFunctions.itemTemplateSpecificDirectory}\{inheritableTrait}.xml").Close();
                    File.WriteAllText($@"{UtilityFunctions.itemTemplateSpecificDirectory}\{inheritableTrait}.xml", listOfFinalXMLs[traitIndex]);
                    traitIndex++;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not write to file: {e}");
                }
            }
            
            
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "Item Factory Initialised", UtilityFunctions.typeSpeed);
        }

        static Item createItem(ItemTemplate template)
        {
            return new Item();
        }

        static ItemTemplate createItemTemplate(string type)
        {
            return new ItemTemplate();
        }
    }
    
    public class ItemTemplate
    {
        public string Name { get; set; }
        public int Description { get; set; }
        public int Value { get; set; }
        public int ItemID { get; set; }
    }
    
    public class Item
    {
        public string Name { get; set; }
        public int Description { get; set; }
        public int Value { get; set; }
        public int ItemID { get; set; }
    }

    public class Weapon : Item
    {
        public int Damage { get; set; }
    }

    public class Consumable : Item
    {
        public int Health { get; set; }
        public int Mana { get; set; }
    }
    
    public class Armour : Item
    {
        public int Defense { get; set; }
    }
}
