using System;
using System.Collections.Generic;
using System.Linq;
using NHunspell;




namespace PuzzleCreator{
    class Program{
        private static readonly Hunspell hunspell = new Hunspell("en_us.aff", "en_us.dic");
    
    
        //                        GLOBALS
        //-------------------------------------------------------
        private static readonly Grammar grammar = new Grammar();
    
        private static readonly List<string> oneLetterWords = new List<string>(){
            "a",
            "i"
        };
    
        private static readonly List<string> twoLetterWords = new List<string>(){
            "ad",
            "am",
            "an",
            "as",
            "at",
            "be",
            "by",
            "do",
            "go",
            "ha",
            "he",
            "hi",
            "if",
            "in",
            "is",
            "it",
            "me",
            "my",
            "no",
            "of",
            "ok",
            "on",
            "or",
            "so",
            "to",
            "up",
            "us",
            "we"
        };
        private static readonly List<string> alreadyPrinted = new List<string>();
    
        private static string startTime;
        //-------------------------------------------------------
    
                
        
        public static void Main(){
    
            //Console.BufferHeight = Int16.MaxValue - 1;
            //Console.WindowWidth = Console.LargestWindowWidth;
            //Console.WindowHeight = Console.LargestWindowHeight;
    
            //Get input
            Console.WriteLine("Enter a string: ");
            var str = Console.ReadLine()?.ToLower();
    
            //Process input
            MainLoop(str);
            PrintAnswers();
    
            //Reprint input, and display timing information
            Console.WriteLine("\n\n----------------------------------------");
            Console.WriteLine("Number of results: " + alreadyPrinted.Count);
    
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Input:  " + "\"" + str + "\"");
            if (str != null) Console.WriteLine("Char length: " + str.Length);
    
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Start time   " + startTime);
            Console.WriteLine("End time     " + DateTime.Now.ToString("h:mm:ss tt"));
    
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Press enter to quit...");
            Console.ReadLine();
            //----------------------------------------------
        }
    
    
    
        /* Iterates over all possible deletion permutations from (1) to (2^n - 1) of the input string.
         * Calls parseString() once for each permutation.
         */
        private static void MainLoop(string str){
            string temp = str;
            int len = str.Length;
    
            Console.WriteLine("Char length: " + len + "\n");
            startTime = DateTime.Now.ToString("h:mm:ss tt");
            Console.WriteLine("Start time   " + startTime + "\n\n------------------------------------------------------------------------------------------------------------------------");
            DisplayPercent(0);
    
            //Number of permutations possible
            ulong perms = (ulong) 1 << len;
    
            //------Percentage complete data--------
            float percent = 0;
            float percentStep;
            if(len <= 9){
                percentStep = 50;
            } else if(len <= 28){
                percentStep = 1;
            } else{
                percentStep = 0.5f;
            }
            
            ulong loopsPerStep = perms / (ulong) (100 / percentStep);
            //--------------------------------------
    
    
            /* 
             * --------------------------------------
             * Iterator loop: loops over each permutation and checks for validity
             * --------------------------------------
             */
            for (ulong index = 1; index < perms - 1; index++){
    
                /* Remove current permutation's designated characters.
                 * This is done by applying the bitwise representation
                 * of 'index' as a mask on 'str' (temp is a copy of str):
                 *    0 = keep character
                 *    1 = delete character
                 */
                for (var i = len - 1; i >= 0; i--){
                    //remove character if the 'i'th bit of 'index' is 1
                    if ((1 & (index >> i)) == 1){
                        temp = temp.Remove(i, 1);
                    }
                }
    
                //Check permutation's validity, then reset temp
                Permute(temp);
                temp = str;
    
    
                if (index % loopsPerStep == 0){
                    percent += percentStep;
                    DisplayPercent((int) percent);
                }
            }
    
            Console.WriteLine("\n----------------------------------------\n");
        }
    
        //Displays the percent bar while generating hits
        private static void DisplayPercent(int percent){
            Console.Write("\r" + $"{percent,3}" + "% completed ");
    
            Console.Write("[[");
            for (var i = 0; i < 100; i++){
                Console.Write(i < percent ? "-" : " ");
            }
            
            Console.Write("]]");
        }
    
        /* Checks to make sure permutations follow a set of rules, otherwise throw them out:
              - At least two words long
              - Words at least 2 characters long, or specifically allowed 1-letter or 2-letter words
              - All words must be real English
         * If a string satisfies these conditions, next check its grammar
         */
        private static void Permute(string str){
            string[] subStrings = str.Split(null);
            int numWords = subStrings.Length;
            //Two words minimum
            if (numWords < 2){
                return;
            }

            //-----------LOOP----------
            //Check each word in the string seperately
            bool allWords = true;
            foreach (var cur in subStrings){
                //Words must be specifically allowed 1-letter or 2-letter words, or in the dictionary
                if (oneLetterWords.Contains(cur) || twoLetterWords.Contains(cur)){
                    //nothing
                } else if( cur.Length > 2 && hunspell.Spell(cur) ){
                    //nothing
                } else{
                    allWords = false;
                    break;
                }
            }

            if( allWords ){
                grammar.answers.Add(str);
            }
        }
            
    
        /* Checks whether each string in List<string> answer has been printed already.
         * If so, move on; otherwise, print it and record that it's been printed.
         */
        private static void PrintAnswers(){
            foreach (string cur in grammar.answers){
                if (!alreadyPrinted.Contains(cur)){
                    var toPrint = char.ToUpper(cur.First()) + cur.Substring(1).ToLower();
                    Console.WriteLine(toPrint);
                    alreadyPrinted.Add(cur);
                }
            }
        }

        
        
    }
}




