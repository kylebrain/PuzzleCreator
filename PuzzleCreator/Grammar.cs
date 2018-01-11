using System.Collections.Generic;
using System.Linq;
using java.io;
using edu.stanford.nlp.process;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.trees;
using edu.stanford.nlp.parser.lexparser;
using ikvm.extensions;
using Console = System.Console;

namespace PuzzleCreator
{
    public class Grammar
    {
        public List<string> sentences = new List<string>();
        public List<string> answers = new List<string>();

        public void GrammarMain()
        {
            // Path to models extracted from `stanford-parser-3.8.0-models.jar`
            var jarRoot = "..\\..\\stanford-parser-full-2017-06-09\\stanford-parser-3.8.0-models";
            var modelsDirectory = jarRoot + "\\edu\\stanford\\nlp\\models";

            // Loading english PCFG parser from file
            var lp = LexicalizedParser.loadModel(modelsDirectory + "\\lexparser\\englishPCFG.ser.gz");

            foreach (string sen in sentences)
            {
                string senF = sen;
                bool isSentence = false;

                var sent = sen;
                var tokenizerFactory = PTBTokenizer.factory(new CoreLabelTokenFactory(), "");
                var sentReader = new StringReader(sent);
                var rawWords = tokenizerFactory.getTokenizer(sentReader).tokenize();
                sentReader.close();
                var tree = lp.apply(rawWords);
                string strTree = tree.ToString();

                    if (strTree.Contains("(S "))
                    {
                        senF = sen + ".";
                        isSentence = true;
                    }
                    else if (strTree.Contains("(SINV ") || strTree.Contains("(SBARQ ") || strTree.Contains("(SQ "))

                    {
                        senF = sen + "?";
                        isSentence = true;
                    }

                    if (isSentence)
                    {
                        var tlp = new PennTreebankLanguagePack();
                        var gsf = tlp.grammaticalStructureFactory();
                        var gs = gsf.newGrammaticalStructure(tree);
                        var tdl = gs.typedDependenciesCCprocessed();
                        string strRel = tdl.ToString();

                        if (strRel.Contains("nsubj("))
                        {
                            answers.Add(senF);
                        }
                    }
                }
            }

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
                    answers.Add(str);
                }
            }

            /* Checks whether a string contains a part of sentence (subject or participle).
             * Base can be "n" or "v" for noun or verb respectively.
             * Modifier can be "a" or "r" for adjective or adverb respectively
             * Returns -1 on error, otherwise returns the index of the last character in the part of speech.
             */
            private static int partOfSentence_analyzer(string[] ps, string b, string mod, int i)
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
            private static bool HasMatch(string input, string desired)
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
        }
    }