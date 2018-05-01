

namespace PuzzleCreator
{
    using System.Collections.Generic;
    using java.io;
    using edu.stanford.nlp.process;
    using edu.stanford.nlp.trees;
    using edu.stanford.nlp.parser.lexparser;
    using Console = System.Console;

    public class Grammar
    {
        /// <summary>
        /// Checks the a list of sentences for correct grammar. Returns a new list of the sentences with correct grammar.
        /// </summary>
        /// <param name="sentences">A list of strings that will have their grammar checked.</param>
        /// <returns>A new list of the sentences with correct grammar.</returns>
        public static HashSet<string> CheckGrammer(HashSet<string> sentences)
        {
            HashSet<string> answers = new HashSet<string>();


            Console.WriteLine(sentences.Count + " potential sentences\n");
            if (sentences.Count == 0)
            {
                return null;
            }

            Console.WriteLine("Loading Stanford NLP...");
            // Loading english PCFG parser from file
            var lp = LexicalizedParser.loadModel("..\\..\\..\\packages\\englishPCFG.ser.gz");
            Console.WriteLine("Stanford Parser Loaded!\n");

            //Test the grammar of each potential sentence that has all english words
            foreach (var curSentence in sentences)
            {
                //Convert the sentence to a tree that Stanford NLP library generates from its parsing
                var tempSentence = curSentence;
                var sentReader = new StringReader(tempSentence);
                var tree = lp.apply(PTBTokenizer.factory(new CoreLabelTokenFactory(), "").getTokenizer(sentReader).tokenize());
                sentReader.close();

                //Determine if the word is a sentence
                string strTree = tree.ToString();
                bool isSentence = false;

                if (strTree.Contains("(S "))
                {
                    tempSentence = curSentence + ".";
                    isSentence = true;
                }
                else if (strTree.Contains("(SINV ") || strTree.Contains("(SBARQ ") || strTree.Contains("(SQ "))
                {
                    tempSentence = curSentence + "?";
                    isSentence = true;
                }

                if (isSentence)
                {
                    var tlp = new PennTreebankLanguagePack();
                    string strRel = tlp.grammaticalStructureFactory().newGrammaticalStructure(tree).typedDependenciesCCprocessed().ToString();

                    if (strRel.Contains("nsubj("))
                    {
                        answers.Add(tempSentence);
                    }
                }
            }
            return answers;
        }

#if OLD_GRAMMAR
        /* Apply grammar rules (must have subject and participle) to ensure real sentences are generated.
         */
        public void GrammarCheck(string str, string[] partOfSpeech_array)
        {
            bool isSentence = false;
            bool isQuestion = false;
            int len = partOfSpeech_array.Length;

            /*
             * a: article
             * v: verb
             * n: noun
             * j: adjective
             * r: adverb
             * i: preposition
             * c: conjunction
             * p: pronoun
             * m: numbers
             */

            //Check subject then participle
            var endIndex = partOfSentence_analyzer(partOfSpeech_array, "n p", "j a m d", 0);
            if ((endIndex > -1) && (endIndex + 1 <= len - 1))
            {
                if (partOfSentence_analyzer(partOfSpeech_array, "v", "r h", endIndex + 1) == len - 1)
                {
                    isSentence = true;
                }
            }

            //If that's false, check participle then subject
            if (!isSentence)

            {
                endIndex = partOfSentence_analyzer(partOfSpeech_array, "v", "r h", 0);
                if ((endIndex > -1) && (endIndex + 1 <= len - 1))
                {
                    if (partOfSentence_analyzer(partOfSpeech_array, "n p", "j a m d", endIndex + 1) == len - 1)
                    {
                        isQuestion = true;
                    }
                }
            }

            if (isSentence)
            {
                str += ".";
            }
            else if (isQuestion)
            {
                str += "?";
            }
            else
            {
                return;
            }

            string last = partOfSpeech_array[len - 1];
            if (!HasMatch(last, "a") && !HasMatch(last, "m") && (last != "u") && (last != "x"))
            {
                //answers.Add(str);
            }
        }

        /* Checks whether a string contains a part of sentence (subject or participle).
         * Base can be "n" or "v" for noun or verb respectively.
         * Modifier can be "a" or "r" for adjective or adverb respectively
         * Returns -1 on error, otherwise returns the index of the last character in the part of speech.
         */
        private int partOfSentence_analyzer(string[] ps, string b, string mod, int i)
        {
            int ret = -1;
            int lastIndex = ps.Length - 1;

            //1-word pattern
            //n
            //v
            bool firstIsBase = HasMatch(ps[i], b);
            if (firstIsBase)
            {
                ret = i;
            }

            if ((i + 1) > lastIndex)
            {
                return ret;
            }

            //2-word pattern
            //a n
            //r v
            bool firstIsMod = HasMatch(ps[i], mod);
            bool secIsBase = HasMatch(ps[i + 1], b);
            if (firstIsMod && secIsBase)
            {
                ret = i + 1;
            }

            if ((i + 2) > lastIndex)
            {
                return ret;
            }

            //3-word pattern
            //n c n
            //v c v
            bool secIsC = HasMatch(ps[i + 1], "c");
            bool thirdIsBase = HasMatch(ps[i + 2], b);
            if (firstIsBase && secIsC && thirdIsBase)
            {
                ret = i + 2;
            }

            if ((i + 3) > lastIndex)
            {
                return ret;
            }

            //4-word patterns
            //n c a n OR a n c n
            //v c r v OR r v c v
            bool thirdIsMod = HasMatch(ps[i + 2], mod);
            bool fourthIsBase = HasMatch(ps[i + 3], b);
            bool thirdIsC = HasMatch(ps[i + 2], "c");
            if (firstIsBase && secIsC && thirdIsMod && fourthIsBase)
            {
                ret = i + 3;
            }
            else if (firstIsMod && secIsBase && thirdIsC && fourthIsBase)
            {
                ret = i + 3;
            }

            if ((i + 4) > lastIndex)
            {
                return ret;
            }

            //5-word pattern
            //a n c a n
            //r v c r v
            bool fourthIsMod = HasMatch(ps[i + 3], mod);
            bool fifthIsBase = HasMatch(ps[i + 4], b);
            if (firstIsMod && secIsBase && thirdIsC && fourthIsMod && fifthIsBase)
            {
                ret = i + 4;
            }

            //Return... finally :P
            return ret;
        }

        /* Checks if the desired string is one of the tags for the input word
         */
        private bool HasMatch(string input, string desired)
        {
            string[] words = input.Split(null);
            string[] pos = desired.Split(null);
            bool isEqual = false;
            foreach (string cur in words)
            {
                if (pos.Contains(cur))
                {
                    isEqual = true;
                }
            }

            return isEqual;
        }
#endif
    }

}