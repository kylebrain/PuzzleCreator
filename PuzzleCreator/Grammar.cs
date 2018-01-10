﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PuzzleCreator
{
    
    public class Grammar
    {
        public List<string> answers = new List<string>();
                
        /* Apply grammar rules (must have subject and participle) to ensure real sentences are generated.
         */
        public void grammarCheck( string str, string[] partOfSpeech_array ) {
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
            int endIndex;
            endIndex = partOfSentence_analyzer(partOfSpeech_array, "n", "j", 0);
            if((endIndex > -1) && (endIndex + 1 <= len - 1)) {
                if(partOfSentence_analyzer(partOfSpeech_array, "v", "r", endIndex + 1) == len - 1) {
                    isSentence = true;
                }
            }
            //If that's false, check participle then subject
            if(!isSentence) {
                endIndex = partOfSentence_analyzer(partOfSpeech_array, "v", "r", 0);
                if((endIndex > -1) && (endIndex + 1 <= len - 1)) {
                    if(partOfSentence_analyzer(partOfSpeech_array, "n", "j", endIndex + 1) == len - 1) {
                        isQuestion = true;
                    }
                }
            }
            if(isSentence) {
                str += ".";
            } else if(isQuestion) {
                str += "?";
            } else {
                return;
            }
            string last = partOfSpeech_array[len - 1];
            if(!hasMatch(last, "a") && !hasMatch(last, "m") && (last != "u") && (last != "x") ){
                answers.Add(str);
            }
        }

        /* Checks whether a string contains a part of sentence (subject or participle).
         * Base can be "n" or "v" for noun or verb respectively.
         * Modifier can be "a" or "r" for adjective or adverb respectively
         * Returns -1 on error, otherwise returns the index of the last character in the part of speech.
         */
        private static int partOfSentence_analyzer( string[] ps, string b, string mod, int i ) {
            int ret = -1;
            int lastIndex = ps.Length - 1;
            
            //1-word pattern
            //n
            //v
            bool firstIsBase = hasMatch(ps[i], b);
            if(firstIsBase) {
                ret = i;
            }
            if((i + 1) > lastIndex) {
                return ret;
            }

            //2-word pattern
            //a n
            //r v
            bool firstIsMod = hasMatch(ps[i], mod);
            bool secIsBase = hasMatch(ps[i + 1], b);
            if(firstIsMod && secIsBase) {
                ret = i + 1;
            }
            if((i + 2) > lastIndex) {
                return ret;
            }

            //3-word pattern
            //n c n
            //v c v
            bool secIsC = hasMatch(ps[i + 1], "c");
            bool thirdIsBase = hasMatch(ps[i + 2], b);
            if(firstIsBase && secIsC && thirdIsBase) {
                ret = i + 2;
            }
            if((i + 3) > lastIndex) {
                return ret;
            }

            //4-word patterns
            //n c a n OR a n c n
            //v c r v OR r v c v
            bool thirdIsMod = hasMatch(ps[i + 2], mod);
            bool fourthIsBase = hasMatch(ps[i + 3], b);
            bool thirdIsC = hasMatch(ps[i + 2], "c");
            if(firstIsBase && secIsC && thirdIsMod && fourthIsBase) {
                ret = i + 3;
            } else if(firstIsMod && secIsBase && thirdIsC && fourthIsBase) {
                ret = i + 3;
            }
            if((i + 4) > lastIndex) {
                return ret;
            }

            //5-word pattern
            //a n c a n
            //r v c r v
            bool fourthIsMod = hasMatch(ps[i + 3], mod);
            bool fifthIsBase = hasMatch(ps[i + 4], b);
            if(firstIsMod && secIsBase && thirdIsC && fourthIsMod && fifthIsBase) {
                ret = i + 4;

            }
            //Return... finally :P
            return ret;
        }

        /* Checks if the desired string is one of the tags for the input word
         */
        public static bool hasMatch( string input, string desired ) {
            string[] words = input.Split(null);
            bool isEqual = false;
            foreach(string cur in words) {
                if(cur == desired) {
                    isEqual = true;
                }
            }
            return isEqual;
        }
    }
}