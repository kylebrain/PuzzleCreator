


namespace PuzzleCreator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data.SQLite;

    public class Program
    {
        private bool wordSkip;
        private readonly Grammar grammar = new Grammar();

        private Dictionary<string, int> sentencesFreq = new Dictionary<string, int>();
        private HashSet<string> sentences = new HashSet<string>();

        private readonly HashSet<string> oneLetterWords = new HashSet<string>() {
            "a",
            "i"
        };

        private HashSet<string> dict = new HashSet<string>();
        private Dictionary<string, int> dictFreq = new Dictionary<string, int>();

        private readonly HashSet<string> twoLetterWords = new HashSet<string>(){
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
        private readonly HashSet<string> alreadyPrinted = new HashSet<string>();

        private string startTime;

        private int countFreq;

        private SQLiteConnection m_dbConnection;
        //-------------------------------------------------------


        /// <summary>
        /// Gets a sentence from the user, and performs all possible character deletion 
        /// permutations on it. It then checks which of those yield strings of valid 
        /// english words. It then checks the grammar for those sentences, and prints 
        /// the ones which are real.
        /// </summary>
        public void Run()
        {

#if BUILD_VERSION
            Console.BufferHeight = Int16.MaxValue - 1;
            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;
#endif

            //establish SQL connection
            m_dbConnection = new SQLiteConnection("Data Source=../../../packages/dictionary.db;Version=3");
            m_dbConnection.Open();

            string countFreqStr;

            do
            {

                Console.WriteLine("Enter frequency count (500 - 10,000): ");
                countFreqStr = Console.ReadLine()?.ToLower();

            } while (!int.TryParse(countFreqStr, out countFreq) || countFreq < 500 || countFreq > 10000);

            InitList(countFreq);

            //Get input
            Console.WriteLine("Enter a string: ");
            var str = Console.ReadLine()?.ToLower();

            //Process input
            Process(str);

            HashSet<string> answers = Grammar.CheckGrammer(sentences);
            if (answers != null)
            {
                PrintAnswers(answers);
            }
            else
            {
                Console.WriteLine("No valid sentences.");
            }


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

        /// <summary>
        /// Initializes the sql database which contains (frequency, word) pairings.
        /// </summary>
        /// <param name="count"></param>
        private void InitList(int count)
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

        private float GetFreqAvg(HashSet<string> words)
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


        /// <summary>
        /// Iterates over all possible deletion permutations from (1) to (2^n - 1) of the input string. Calls parseString() once for each permutation.
        /// </summary>
        /// <param name="str">The string to be tested</param>
        private void Process(string str)
        {
            string temp = str;
            int len = str.Length;

            Console.WriteLine("Char length: " + len + "\n");
            startTime = DateTime.Now.ToString("h:mm:ss tt");
            Console.WriteLine("Start time   " + startTime);
            Console.WriteLine("\n----------------------------------------\n");


            //Number of permutations possible
            ulong perms = (ulong)1 << len;

            //------Percentage complete data--------
            ulong loopsPerStep = perms / (ulong)(100);
            float percent = 0f;

            /* 
             * --------------------------------------
             * Iterator loop: loops over each permutation and checks for validity
             * --------------------------------------
             */
            DisplayPercent(0);
            for (ulong index = 1; index < perms - 1; index++)
            {

                /* Remove current permutation's designated characters.
                 * This is done by applying the bitwise representation
                 * of 'index' as a mask on 'str' (temp is a copy of str):
                 *    0 = keep character
                 *    1 = delete character
                 */
                for (var i = 0; i < len; i++)
                {
                    //remove character if the 'i'th bit of 'index' is 1
                    if ((1 & (index >> i)) == 1)
                    {
                        temp = temp.Remove(len - 1 - i, 1);
                    }
                }

                //Check permutation's validity
                index = CheckEnglish(temp, index, str);

                //Reset temp
                temp = str;

                //Print the percentage complete to the screen
                if (index % loopsPerStep == 0)
                {
                    percent++;
                    DisplayPercent((int)percent);
                }
            }

            DisplayPercent(100);
            Console.WriteLine("\n----------------------------------------\n");
        }

        //Displays the percent bar while generating hits
        private void DisplayPercent(int percent)
        {
            Console.Write("\r" + $"{percent,3}" + "% completed ");

            Console.Write("[[");
            for (var i = 0; i < 100; i++)
            {
                Console.Write(i < percent ? "-" : " ");
            }

            Console.Write("]]");
        }


        /// <summary>
        /// Checks to make sure permutations follow a set of rules, otherwise throw them out.
        /// If a string satisfies these conditions, next check its grammar
        /// </summary>
        /// <remarks>
        /// - At least two words long
        /// - Words at least 2 characters long, or specifically allowed 1-letter or 2-letter words
        /// - All words must be real English
        /// </remarks>
        /// <param name="str">The input string</param>
        /// <param name="perm">The current permutation. Used for short circuting to later permutations</param>
        /// <param name="full">The non-permuted original string</param>
        /// <returns></returns>
        private ulong CheckEnglish(string str, ulong perm, string full)
        {
            string[] subStrings = str.Split(null);
            int numWords = subStrings.Length;

            int checkLen = 0;

            //Two words minimum
            if (numWords < 2)
            {
                return perm;
            }

            //-----------LOOP----------
            //Check to see if all words in the sentence are real
            bool allWords = true;
            wordSkip = true;
            foreach (string cur in subStrings)
            {
                checkLen += cur.Length + 1;

                //Specifically allowed 1-letter or 2-letter words are fine
                if (oneLetterWords.Contains(cur) || twoLetterWords.Contains(cur))
                {
                    continue;
                }

                //Words longer than 2 character that are in the dictionary are fine
                if (cur.Length > 2 && dict.Contains(cur))
                {
                    continue;
                }



                //Otherwise, stop and reset checkLen in preparation for short circuting perm next
                if (cur == subStrings.Last())
                {
                    wordSkip = false;
                }
                checkLen = str.Length - checkLen;
                allWords = false;
                break;
            }

            //If everything is a real word, check the words' frequencies
            if (allWords)
            {
                int freqScore = 0;
                foreach (var cur in subStrings)
                {
                    int temp;
                    dictFreq.TryGetValue(cur, out temp);
                    freqScore += temp;
                }

                if (allWords && !sentencesFreq.Keys.Contains(str))
                {
                    sentencesFreq.Add(str, freqScore / numWords);
                    sentences.Add(str);
                }
            }

            //If not all words are real, calculate how many perms to skip
            if (!allWords && wordSkip && checkLen > 0)
            {
                int index = 0;
                int zeroFound = 0;

                zeroFound = 0;
                while (zeroFound <= checkLen)
                {
                    if (((ulong)(1 << index) & perm) == 0)
                    {
                        zeroFound++;
                    }
                    index++;
                }
                if (index != 0)
                {
                    index--;
                }

                ulong mask;
                if (index > -1 && full[full.Length - index - 1] == ' ')
                {
                    mask = (ulong)1 << (index);
                    ulong newPerm = (perm | mask);
                    newPerm &= (~(ulong)0) << index;
                    int skipped = (int)newPerm - (int)perm;
                    return newPerm - 1;
                }
                else
                {
                    return perm;
                }

            }

            return perm;
        }


        /// <summary>
        /// Prints each string in answers only once. Prints them in ascending order based on how common their words are.
        /// </summary>
        /// <param name="answers"></param>
        private void PrintAnswers(HashSet<string> answers)
        {

            Dictionary<string, int> answerFreq = new Dictionary<string, int>();

            foreach (var cur in answers)
            {
                answerFreq.Add(cur, sentencesFreq[cur.Remove(cur.Length - 1, 1)]);
            }

            var ordered = answerFreq.OrderBy(x => x.Value);
            foreach (var cur in ordered)
            {
                if (!alreadyPrinted.Contains(cur.Key))
                {
                    var toPrint = cur.Value + ": " + char.ToUpper(cur.Key.First()) + cur.Key.Substring(1).ToLower();
                    Console.WriteLine(toPrint);
                    alreadyPrinted.Add(cur.Key);
                }
            }
        }



    }
}




