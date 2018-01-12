using System;
using System.Collections.Generic;
using System.Linq;
using NHunspell;
using System.Data.SQLite;
using System.Runtime.InteropServices;
using com.sun.jndi.url.corbaname;
using com.sun.tools.classfile;


namespace PuzzleCreator{
    class Program{
        private static readonly Hunspell hunspell = new Hunspell("en_us.aff", "en_us.dic");
    
    
        //                        GLOBALS
        //-------------------------------------------------------
        private static readonly Grammar grammar = new Grammar();
        
        private static Dictionary<string, int> sentencesFreq = new Dictionary<string, int>();
        private static HashSet<string> sentences = new HashSet<string>();
    
        private static readonly HashSet<string> oneLetterWords = new HashSet<string>() {
            "a",
            "i"
        };
        
        private static HashSet<string> dict = new HashSet<string>();
        private static Dictionary<string, int> dictFreq = new Dictionary<string, int>();
    
        private static readonly HashSet<string> twoLetterWords = new HashSet<string>(){
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
        private static readonly HashSet<string> alreadyPrinted = new HashSet<string>();
    
        private static string startTime;

        private static int countFreq;
        
        static SQLiteConnection m_dbConnection;
        //-------------------------------------------------------


        private static void InitList(string count)
        {
            string sql = "SELECT word FROM words LIMIT " + count;
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            int n = 0;
            while (reader.Read())
            {
                string word = reader["word"].ToString();

                if (!dictFreq.Keys.Contains(word))
                {
                    dictFreq.Add(word, n);
                    dict.Add(word);
                    n++;
                }
            }
        }

        private static float GetFreqAvg(HashSet<string> words)
        {
            float total = 0;
            int num = 0;
            foreach (var word in words)
            {
                int freq;
                if (dictFreq.TryGetValue(word, out freq))
                {
                    num++;
                    total += freq;
                }
                else
                {
                    Console.WriteLine(word + " not in freq words!");
                }
            }

            return total / num;
        }
        
        public static void Main()
        {
            
            //Console.BufferHeight = Int16.MaxValue - 1;
            //Console.WindowWidth = Console.LargestWindowWidth;
            //Console.WindowHeight = Console.LargestWindowHeight;
            
            //establish SQL connection
            m_dbConnection = new SQLiteConnection("Data Source=../../dictionary.db;Version=3");
            m_dbConnection.Open();

            Console.WriteLine("Enter frequency count (out of 10,000): ");
            string countFreqStr = Console.ReadLine()?.ToLower();
            countFreq = Int32.Parse(countFreqStr);
            
            InitList(countFreqStr);
            
            //Console.WriteLine("ONE letter avg score: " + GetFreqAvg(oneLetterWords));
            //Console.WriteLine("TWO letter avg score: " + GetFreqAvg(twoLetterWords));
    
            //Get input
            Console.WriteLine("Enter a string: ");
            var str = Console.ReadLine()?.ToLower();
    
            //Process input
            MainLoop(str);
            
            grammar.GrammarMain(sentences);
            
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
                if (oneLetterWords.Contains(cur) || twoLetterWords.Contains(cur))
                {
                    //freqScore += dict.IndexOf(cur);
                    //nothing
                    /*} else if( cur.Length > 2 && hunspell.Spell(cur) ){
                        //nothing*/
                    
                } else if( cur.Length > 2 && dict.Contains(cur)){
                    //freqScore += dict.IndexOf(cur);
                    //nothing    
                } else{
                    allWords = false;
                    break;
                }
            }

            if( allWords)
            {
                int freqScore = 0;
                foreach (var cur in subStrings)
                {
                    if (cur.Length > 2)
                    {
                        freqScore += dictFreq[cur];
                    }
                    else if (cur.Length > 1)
                    {
                        freqScore += 35; //average frequency of two letter words
                    }
                    else
                    {
                        freqScore += 8; //average frequency of one letter words
                    }
                }

                if (allWords && !sentencesFreq.Keys.Contains(str))
                {
                    sentencesFreq.Add(str, freqScore / numWords);
                    sentences.Add(str);
                }   
            }
        }
            
    
        /* Checks whether each string in List<string> answer has been printed already.
         * If so, move on; otherwise, print it and record that it's been printed.
         */
        private static void PrintAnswers(){

            Dictionary<string, int> answerFreq = new Dictionary<string, int>();
            
            foreach (var cur in grammar.answers)
            {
                answerFreq.Add(cur, sentencesFreq[cur.Remove(cur.Length - 1, 1)]);
            }
            
            var ordered = answerFreq.OrderBy(x => x.Value);
            foreach (var cur in ordered){
                if (!alreadyPrinted.Contains(cur.Key)){
                    var toPrint = cur.Value + ": " + char.ToUpper(cur.Key.First()) + cur.Key.Substring(1).ToLower();
                    Console.WriteLine(toPrint);
                    alreadyPrinted.Add(cur.Key);
                }
            }
        }

        
        
    }
}




