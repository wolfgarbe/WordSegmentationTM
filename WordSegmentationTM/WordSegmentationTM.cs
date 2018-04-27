// WordSgmentationTM: Fast Word Segmentation with Triangular Matrix 
// Copyright (C) 2018 Wolf Garbe
// Version: 1.1
// Author: Wolf Garbe wolf.garbe@faroo.com
// Maintainer: Wolf Garbe wolf.garbe@faroo.com
// URL: //https://github.com/wolfgarbe/WordSegmentationTM
// Description: https://towardsdatascience.com/fast-word-segmentation-for-noisy-text-2c2c41f9e8da

// MIT License
// Copyright (c) 2018 Wolf Garbe
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class WordSegmentationTM
{
    //The number of all words in the text corpus from which the frequency dictionary was derived
    //Google Books Ngram data: http://storage.googleapis.com/books/ngrams/books/datasetsv2.html
    //The probability P of a word = count of the word in the corpus / number of all words in the corpus 
    public long N = 1024908267229L;

    //dictionary
    public Dictionary<string, long> dictionary = new Dictionary<string, long>();
    public int maximumDictionaryWordLength = 0;

    /// <summary>Create a new instanc of WordSegmentationTM.</summary>
    public WordSegmentationTM()
    {

    }

    /// <summary>Load multiple dictionary entries from a file of word/frequency count pairs</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpus">The path+filename of the file.</param>
    /// <param name="termIndex">The column position of the word.</param>
    /// <param name="countIndex">The column position of the frequency count.</param>
    /// <returns>True if file loaded, or false if file not found.</returns>
    public bool LoadDictionary(string corpus, int termIndex=0, int countIndex=1)
    {
        if (!File.Exists(corpus)) return false;

        using (StreamReader sr = new StreamReader(File.OpenRead(corpus)))
        {
            String line;

            //process a single line at a time only for memory efficiency
            while ((line = sr.ReadLine()) != null)
            {
                string[] lineParts = line.Split(null);
                if (lineParts.Length >= 2)
                {
                    string key = lineParts[termIndex];
                    //Int64 count;
                    if (Int64.TryParse(lineParts[countIndex], out Int64 count))
                    {
                        if (key.Length > maximumDictionaryWordLength) maximumDictionaryWordLength = key.Length;
                        dictionary[key] = count;
                    }
                }
            }
        }

        return true;
    }
    
    /// <summary>Find best word segmentation for input string.</summary>
    /// <param name="input">The string being word segmented.</param>
    /// <returns>A tuple representing the suggested word segmented text and the sum of logarithmic word occurence probabilities.</returns> 
    public (string segmentedString, decimal probabilityLogSum) Segment(string input)
    {
        return Segment(input, maximumDictionaryWordLength);
    }
    
    /// <summary>Find best word segmentation for input string.</summary>
    /// <param name="input">The string being word segmented.</param>
    /// <param name="maxSegmentationWordLength">The maximum word length that should be considered.</param>	
    /// <returns>A tuple representing the suggested word segmented text and the sum of logarithmic word occurence probabilities.</returns> 
    public (string segmentedString, decimal probabilityLogSum) Segment(string input, int maxSegmentationWordLength)
    {
        int arraySize = Math.Min(maxSegmentationWordLength, input.Length);
        int arrayWidth = ((input.Length - 1) >> 6) + 1; // /64 bit
        int arrayWidthByte = arrayWidth << 3; //*8 byte
        //instead of storing the segmented strings, we store only an array of potential space positions: bit set == space (1 bit instead of 1 char)
        ulong[,] segmentedSpaceBits = new ulong[arraySize, arrayWidth];
        decimal[] probabilityLogSum = new decimal[arraySize];
        int circularIndex = -1;

        //A Triangular Matrix of parts is generated (increasing part lengths), organized as Circular Array
        //with nested loops of dimensions n*m : n=input.length; m=Min(input.length,maximum word length) 

        //outer loop (column): all possible part start positions
        for (int j = 0; j < input.Length; j++)
        {
            int spaceUlongIndex = (j - 1) >> 6; // /64 bit
            int arrayCopyByte = Math.Min(((spaceUlongIndex + 1) << 3), arrayWidthByte); // *8 byte

            //best segmentation for the prefix of length j will be combined with + " " + part1; set space bit in row 0
            if (j > 0) segmentedSpaceBits[circularIndex, spaceUlongIndex] |= ((ulong)1 << ((j - 1) & 0x3f)); // %64 bit

            //inner loop (row): all possible part lengths (from start position): part can't be bigger than longest word in dictionary (other than long unknown word)
            int imax = Math.Min(input.Length - j, maxSegmentationWordLength);
            for (int i = 1; i <= imax; i++)
            {
                int destinationIndex = ((i + circularIndex) % arraySize);

                //Calculate the Naive Bayes probability of a sequence of words (iterative in logarithmic scale)
                string part1 = input.Substring(j, i);
                decimal ProbabilityLogPart1 = 0;
                if (dictionary.TryGetValue(part1, out long wordCount)) ProbabilityLogPart1 = (decimal)Math.Log10((double)wordCount / (double)N);
                //estimation for unknown words
                else ProbabilityLogPart1 = (decimal)Math.Log10(10.0 / (N * Math.Pow(10.0, part1.Length)));

                //set values in first loop
                if (j == 0)
                {
                    probabilityLogSum[destinationIndex] = ProbabilityLogPart1;
                }
                //replace values if better probabilityLogSum
                else if ((i == maxSegmentationWordLength) || (probabilityLogSum[destinationIndex] < probabilityLogSum[circularIndex] + ProbabilityLogPart1))
                {
                    System.Buffer.BlockCopy(segmentedSpaceBits, circularIndex * arrayWidthByte, segmentedSpaceBits, destinationIndex * arrayWidthByte, arrayCopyByte);
                    probabilityLogSum[destinationIndex] = probabilityLogSum[circularIndex] + ProbabilityLogPart1;
                }
            }

            circularIndex++; if (circularIndex == arraySize) circularIndex = 0;
        }

        //create segmented string result from input and segmentedSpaceBits
        StringBuilder resultString = new StringBuilder(input.Length * 2);
        int last = -1;
        for (int i = 0; i <= input.Length - 2; i++) if ((segmentedSpaceBits[circularIndex, i >> 6] & ((ulong)1 << (i & 0x3f))) > 0)
            {
                resultString.Append(input, last + 1, i - last);
                resultString.Append(' ');
                last = i;
            }
        return (resultString.Append(input.Substring(last + 1)).ToString(), probabilityLogSum[circularIndex]);
    }

}

