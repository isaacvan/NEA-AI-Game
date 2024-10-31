using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Xml.Serialization;
using MainNamespace;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;
using PlayerClassesNamespace;
using UtilityFunctionsNamespace;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ItemFunctionsNamespace
{
    public class Inventory
    {
        public List<Item> Items { get; set; }
        public List<int> Quantities { get; set; }
        
        public Inventory()
        {
            Items = new List<Item>();
            Quantities = new List<int>();
        }

        public void AddItem(Item item, int quant = 1)
        {
            // add item to inventory
            // if item already exists, increase quantity
            // if item does not exist, add item to inventory

            int index = Items.IndexOf(item);
            if (index != -1)
            {
                // Item exists, increase the quantity
                Quantities[index] += quant;
            }
            else
            {
                // Item does not exist, add new item and set quantity to 1
                Items.Add(item);
                Quantities.Add(quant);
            }
        }

        public void RemoveItem(Item item, int quant = 1)
        {
            int index = Items.IndexOf(item);
            if (index != -1) // checking item exists
            {
                Quantities[index] -= quant;
                if (Quantities[index] <= 0)
                {
                    // Remove the item completely if quantity is 0 or less
                    Items.RemoveAt(index);
                    Quantities.RemoveAt(index);
                }
            }
        }

        public async Task updateInventoryJSON()
        {
            // puts this inventory into JSON file called saveName var in Inventories
            string path = UtilityFunctions.mainDirectory + @"Inventories\" + UtilityFunctions.saveName + ".json";
            using (StreamWriter file = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented // Pretty print
                };
                serializer.Serialize(file, this);
                await file.FlushAsync();
            }
        }
        
        public void updateInventoryJSONSync()
        {
            // puts this inventory into JSON file called saveName var in Inventories
            string path = UtilityFunctions.mainDirectory + @"Inventories\" + UtilityFunctions.saveName + ".json";
            using (StreamWriter file = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented // Pretty print
                };
                serializer.Serialize(file, this);
                file.Flush();
            }
        }
    }

    // EQUIPMENT USAGE
    /*
    item created through factory, e.g
    Item item = itemFactory.createItem(itemFactory.armourTemplates[0]);
    player.EquipItem({"Head", "Body", "Legs", "Weapon", "Accessory"}, item);
    */
    
    public class Equipment
    {
        public Dictionary<EquippableItem.EquipLocation, Item> Slots { get; private set; }

        public Equipment()
        {
            Slots = new Dictionary<EquippableItem.EquipLocation, Item>();
            foreach (var location in  Enum.GetValues<EquippableItem.EquipLocation>())
            {
                Slots.Add(location, null);    
            }
        }
        
        public void EquipItem(EquippableItem.EquipLocation slot, Item item, Inventory inventory)
        {
            if (!Slots.ContainsKey(slot))
            {
                throw new ArgumentException($"No slot named {slot} exists.");
            }
        
            // Check if there is already an item equipped in the slot
            if (Slots[slot] != null)
            {
                // Optionally, automatically unequip the current item to inventory
                UnequipItem(slot, inventory);
            }

            Slots[slot] = item;
            inventory.RemoveItem(item);  // Remove the item from inventory when equipped
        }

        public void UnequipItem(EquippableItem.EquipLocation slot, Inventory inventory)
        {
            if (Slots[slot] != null)
            {
                inventory.AddItem(Slots[slot]);  // Add the unequipped item back to the inventory
                Slots[slot] = null;
            }
        }

        public async Task updateEquipmentJSON()
        {
            // puts this equipment into XML file called saveName var in Equipment
            string path = UtilityFunctions.mainDirectory + @"Equipments\" + UtilityFunctions.saveName + ".json";
            await UtilityFunctions.writeToJSONFile<Equipment>(path, this);
        }
        
        public void updateEquipmentJSONSync()
        {
            // puts this equipment into XML file called saveName var in Equipment
            string path = UtilityFunctions.mainDirectory + @"Equipments\" + UtilityFunctions.saveName + ".json";
            UtilityFunctions.writeToJSONFileSync<Equipment>(path, this);
        }
    }
    
    // ITEMFACTORY USAGE
    /*
    Item NEWITEMINSTANCE = itemFactory.createItem(itemFactory.{"armour", "weapon", "consumable"}Templates[index]);
    */
     
    
    
    public class ItemFactory
    {
        public List<WeaponTemplate> weaponTemplates { get; set; } = new List<WeaponTemplate>();
        public List<ConsumableTemplate> consumableTemplates { get; set; } = new List<ConsumableTemplate>();
        public List<ArmourTemplate> armourTemplates { get; set; } = new List<ArmourTemplate>();

        public async Task initialiseItemFactoryFromFile()
        {
            Program.logger.Info("Initialising Item Factory...");

            if (UtilityFunctions.saveName == "saveExample")
            {
                UtilityFunctions.itemTemplateSpecificDirectory =
                    UtilityFunctions.itemTemplateDir + UtilityFunctions.saveName + "s";
            }
            else
            {
                UtilityFunctions.itemTemplateSpecificDirectory =
                    UtilityFunctions.itemTemplateDir + UtilityFunctions.saveName;
            }
            
            // load item templates from file
            // FOLLOW LOGIC OF OTHER ONE BUT JUST LOAD AN ITEM FACTORY FROM ONE FILE
            
            // initialise xml files into respective item templates HERE
            ItemContainer itemContainer = new ItemContainer();
            
            
            var armourItems = ItemContainerUtility.DeserializeItemsFromFile($@"{UtilityFunctions.itemTemplateSpecificDirectory}{Path.DirectorySeparatorChar}Armour.xml").Armours;
            var weaponItems = ItemContainerUtility.DeserializeItemsFromFile($@"{UtilityFunctions.itemTemplateSpecificDirectory}{Path.DirectorySeparatorChar}Weapon.xml").Weapons;
            var consumableItems = ItemContainerUtility.DeserializeItemsFromFile($@"{UtilityFunctions.itemTemplateSpecificDirectory}{Path.DirectorySeparatorChar}Consumable.xml").Consumables;
            //itemContainer.Armours = new List<Armour>();
            // convert to itemTemplates
            
            
            // add to item factory
            foreach (Armour armour in armourItems)
            {
                ArmourTemplate template = new ArmourTemplate();
                armour.ItemType = typeof(Armour);
                template.createTemplate(armour);
                this.armourTemplates.Add(template);
            }
            foreach (Weapon weapon in weaponItems)
            {
                WeaponTemplate template = new WeaponTemplate();
                weapon.ItemType = typeof(Weapon);
                template.createTemplate(weapon);
                this.weaponTemplates.Add(template);
            }
            foreach (Consumable consumable in consumableItems)
            {
                ConsumableTemplate template = new ConsumableTemplate();
                consumable.ItemType = typeof(Consumable);
                template.createTemplate(consumable);
                this.consumableTemplates.Add(template);
            }
        }

        public async Task initialiseItemFactoryFromNarrator(OpenAIAPI api, Conversation chat, bool testing = false)
        {
            Program.logger.Info("Initialising Item Factory...");
            
            // initialise path, should be fine with testing
            if (!testing)
            {
                UtilityFunctions.itemTemplateSpecificDirectory =
                    UtilityFunctions.itemTemplateDir + UtilityFunctions.saveName;
            }
            else
            {
                UtilityFunctions.itemTemplateSpecificDirectory =
                    UtilityFunctions.itemTemplateDir + UtilityFunctions.saveName + "s";
            }

            // load item templates from narrator
            string prompt6 = File.ReadAllText($"{UtilityFunctions.promptPath}Prompt6.txt");
            
            // get response from GPT
            string output = "";
            if (!testing)
            {
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
                        File.Create($@"{UtilityFunctions.itemTemplateSpecificDirectory}\{inheritableTrait}.xml")
                            .Close();
                        File.WriteAllText($@"{UtilityFunctions.itemTemplateSpecificDirectory}\{inheritableTrait}.xml",
                            listOfFinalXMLs[traitIndex]);
                        traitIndex++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not write to file: {e}");
                    }
                }
            }
            
            
            // initialise xml files into respective item templates HERE
            ItemContainer itemContainer = new ItemContainer();
            
            
            var armourItems = ItemContainerUtility.DeserializeItemsFromFile($@"{UtilityFunctions.itemTemplateSpecificDirectory}{Path.DirectorySeparatorChar}Armour.xml").Armours;
            var weaponItems = ItemContainerUtility.DeserializeItemsFromFile($@"{UtilityFunctions.itemTemplateSpecificDirectory}{Path.DirectorySeparatorChar}Weapon.xml").Weapons;
            var consumableItems = ItemContainerUtility.DeserializeItemsFromFile($@"{UtilityFunctions.itemTemplateSpecificDirectory}{Path.DirectorySeparatorChar}Consumable.xml").Consumables;
            //itemContainer.Armours = new List<Armour>();
            // convert to itemTemplates
            
            
            // add to item factory
            foreach (Armour armour in armourItems)
            {
                ArmourTemplate template = new ArmourTemplate();
                armour.ItemType = typeof(Armour);
                template.createTemplate(armour);
                this.armourTemplates.Add(template);
            }
            foreach (Weapon weapon in weaponItems)
            {
                WeaponTemplate template = new WeaponTemplate();
                weapon.ItemType = typeof(Weapon);
                template.createTemplate(weapon);
                this.weaponTemplates.Add(template);
            }
            foreach (Consumable consumable in consumableItems)
            {
                ConsumableTemplate template = new ConsumableTemplate();
                consumable.ItemType = typeof(Consumable);
                template.createTemplate(consumable);
                this.consumableTemplates.Add(template);
            }
            
            
            // initialise
            

            Program.logger.Info("Item Factory Initialised");
        }

        public Item createItem(ItemTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template), "The item template cannot be null.");
            }

            if (template.ItemType == null)
            {
                throw new ArgumentException("Invalid item type specified in the template.");
            }

            Type itemType = template.getItemTypeFromTemplate();
            
            //Console.WriteLine($"Item type: {itemType.Name}");

            //Type itemType = typeof(Item);
            PropertyInfo[] properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Item newItem = (Item)Activator.CreateInstance(itemType);

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    // Get the property from the template by name
                    PropertyInfo templateProperty = template.GetType().GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);
                    if (templateProperty != null && templateProperty.CanRead)
                    {
                        object value = templateProperty.GetValue(template);  // Get value from the template

                        // Check if the types are compatible
                        if (property.PropertyType.IsAssignableFrom(templateProperty.PropertyType))
                        {
                            property.SetValue(newItem, value);  // Set value on the new item
                        }
                        else
                        {
                            Console.WriteLine($"Type mismatch for property {property.Name}: {property.PropertyType} expected, but got {templateProperty.PropertyType}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property in createItem {property.Name}: {ex.Message}");
                }
            }

            return newItem;
        }
    }
    
    public abstract class ItemTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type ItemType { get; set; }
        public EquippableItem.EquipLocation ItemEquipLocation { get; set; }

        public void createTemplate(Item item)
        {
            // set player properties
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (item.ItemType == null) throw new ArgumentNullException(nameof(item));

            Type itemType = item.GetType();
            PropertyInfo[] properties = itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance);


            foreach (PropertyInfo property in properties)
            {
                /*
                if (property.Name == "Name")
                {
                    this.Name = item.Name;
                }
                else if (property.Name == "Description")
                {
                    this.Description = item.Description;
                }
                else if (property.Name == "ItemEquipLocation")
                {
                    this.ItemEquipLocation = item.ItemEquipLocation;
                }
                else if (property.Name == "ItemType")
                {
                    this.ItemType = itemType.GetType();
                }
                else
                {
                    try
                    {
                        object value = property.GetValue(item);
                        property.SetValue(this, value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to set property in createTemplate {property.Name}: {ex.Message}");
                        // Handle or log the error as necessary
                    }
                }
                */
                try
                {
                    object value = property.GetValue(item);
                    PropertyInfo thisProperty = this.GetType().GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance);

                    if (thisProperty != null && thisProperty.CanWrite)
                    {
                        thisProperty.SetValue(this, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property in createItemTemplate {property.Name}: {ex.Message}");
                }
            }
        }

        public Type getItemTypeFromTemplate()
        {
            Type templateType = this.GetType();
            string templateTypeString = templateType.Name;
            string itemTypeString = templateTypeString;

            if (templateTypeString.EndsWith("Template", StringComparison.Ordinal))
            {
                itemTypeString = templateTypeString.Substring(0, templateTypeString.Length - 8);
            }
            else
            {
                throw new Exception($"Invalid item type specified in the template: {templateTypeString}");
            }
            
            try
            {
                // Assembly assembly = Assembly.Load("__ItemTypes");
                // Type itemType = Type.GetType($"__ItemTypes.{itemTypeString}, __ItemTypes", throwOnError: true);
                // bin/Debug/net7.0/Code Projects.dll
                string assemblyPath = @$"{UtilityFunctions.mainDirectory}bin/Debug/net7.0/Code Projects.dll"; // Adjust the path as necessary
                Assembly asm = Assembly.LoadFrom(assemblyPath);
                Type itemType = asm.GetType($"ItemFunctionsNamespace.{itemTypeString}", throwOnError: true); // Replace Namespace with the actual namespace
                return itemType;
                // Use itemType confidently here
            }
            catch (TypeLoadException ex)
            {
                // Handle cases where the type could not be loaded
                throw new Exception($"Error loading type: {ex.Message}");
            }
        }

        public void createItemInstance()
        {
            // create item instance
            //Item item = ItemFactory.createItem(this);
        }
    }
    
    public class WeaponTemplate : ItemTemplate
    {
        public int Damage { get; set; }
        public string WeaponType { get; set; }
        public string UniqueProperties { get; set; }
        public string AttackBehaviour { get; set; }
        public List<string> StatusNames { get; set; } = new List<string>();
        public string NarrativeLine { get; set; }
    }

    public class ConsumableTemplate : ItemTemplate
    {
        public int Health { get; set; }
        public int Mana { get; set; }
        public string Effect { get; set; }
        public string UniqueProperties { get; set; }
        public string StorylineRelevance { get; set; }
    }

    public class ArmourTemplate : ItemTemplate
    {
        public int Defence { get; set; }
        public string DefensiveCapabilities { get; set; }
        public string UniqueProperties { get; set; }
    }
    
    [XmlRoot("Item")]
    public class ItemContainer
    {
        [XmlElement("Armour")]
        public List<Armour> Armours { get; set; } = new List<Armour>();
        
        [XmlElement("Weapon")]
        public List<Weapon> Weapons { get; set; } = new List<Weapon>();
        
        [XmlElement("Consumable")]
        public List<Consumable> Consumables { get; set; } = new List<Consumable>();
        
        
    }

    public static class ItemContainerUtility
    {
        public static ItemContainer DeserializeItemsFromFile(string filePath)
        {
            try
            {
                UtilityFunctions.CorrectXmlTags(filePath);
                XmlSerializer serializer = new XmlSerializer(typeof(ItemContainer));

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    return (ItemContainer)serializer.Deserialize(fileStream);
                    // Now you can work with the data in itemContainer.Armours
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
    
    public abstract class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Type ItemType { get; set; }
        
    }

    public abstract class EquippableItem : Item
    {
        public enum EquipLocation
        {
            Head,
            Body,
            Legs,
            Weapon,
            Accessory,
        }
    }

    
    public class Weapon : EquippableItem
    {
        public int Damage { get; set; }
        public string WeaponType { get; set; }
        public string UniqueProperties { get; set; }
        public string AttackBehaviour { get; set; }
        public List<string> StatusNames { get; set; } = new List<string>();
        public string NarrativeLine { get; set; }
    }

    public class Consumable : Item
    {
        public int Health { get; set; }
        public int Mana { get; set; }
        public string Effect { get; set; }
        public string UniqueProperties { get; set; }
        public string StorylineRelevance { get; set; }
    }

    public class Armour : EquippableItem
    {
        public int Defence { get; set; }
        public string DefensiveCapabilities { get; set; }
        public string UniqueProperties { get; set; }
    }
    
    
}
