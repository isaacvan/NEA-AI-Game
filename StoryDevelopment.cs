using UtilityFunctionsNamespace;
using PlayerClassesNamespace;
using EnemyClassesNamespace;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace StoryDevelopmentNamespace
{
    public class StoryDevelopment
    {
        public static void story1(string chosenClass, Player player)
        { // main path the character goes. at the start will take a random choice and go down one of the roots. chatgpt??



        }

        public static void story2(string chosenClass, Player player)
        {



            secondContinuation(chosenClass, player);


            UtilityFunctions.lobby(player);
            Console.ReadLine();
        }





        public static void secondContinuation(string chosenClass, Player player)
        {
            string continuation = "";

            if (chosenClass == "Warrior")
            {
                continuation = "\x1b[31mAfter vanquishing their foe, the \x1b[32mWarrior\x1b[31m discovers a gleaming sword on the battlefield. Recognizing its potential, they claim the weapon as their own.\x1b[0m";
                UtilityFunctions.givePlayerItem("sword", player);
            }
            else if (chosenClass == "Mage")
            {
                continuation = "\x1b[34mWith the enemy defeated, the \x1b[32mMage\x1b[34m uncovers an ancient staff hidden among the fallen leaves. Sensing its arcane power, they take it as their own.\x1b[0m";
                UtilityFunctions.givePlayerItem("staff", player);
            }
            else if (chosenClass == "Rogue")
            {
                continuation = "\x1b[33mAs the dust settles, the \x1b[32mRogue\x1b[33m finds a pair of razor-sharp daggers lying beside their vanquished enemy. Intrigued by their deadly precision, they claim the daggers as their own.\x1b[0m";
                UtilityFunctions.givePlayerItem("daggers", player);
            }

            UtilityFunctions.TypeText(UtilityFunctions.Instant, "\n" + continuation + "\n\n", UtilityFunctions.typeSpeed);
        }





        public static void exploreFurther(Player player)
        {
            //UtilityFunctions.TypeText(UtilityFunctions.Instant, "\n\x1b[35mPress anything to explore further.\n'lobby' will send you to the lobby.\x1b[0m\n\n", UtilityFunctions.typeSpeed);
            UtilityFunctions.TypeText(UtilityFunctions.Instant, "\n\x1b[35mPress anything to explore further.\x1b[0m\n\n", UtilityFunctions.typeSpeed);
            string input = Console.ReadLine();
            if (input == "lobby")
            {
                UtilityFunctions.lobby(player);
            }
        }
    }
}