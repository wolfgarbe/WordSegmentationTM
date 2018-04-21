// Copyright (C) 2018 Wolf Garbe
// Version: 1.0
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


//The number of all words in the text corpus from which the frequency dictionary was derived
//Google Books Ngram data: http://storage.googleapis.com/books/ngrams/books/datasetsv2.html
//The probability P of a word = count of the word in the corpus / number of all words in the corpus 
public static long N = 1024908267229L;

//dictionary
public static Dictionary<string, long> dictionary = new Dictionary<string, long>();
public static int maximumDictionaryWordLength = 0;

//Read word, word frequency pairs from dictionary file.
public static bool LoadDictionary(String path)
{
    String line;
    String[] word;

    if (!File.Exists(path)) return false;

    using (StreamReader br = new StreamReader(File.OpenRead(path)))
    {
        while ((line = br.ReadLine()) != null)
        {
            word = line.Split(null);
            if (word[0].Length > maximumDictionaryWordLength) maximumDictionaryWordLength = word[0].Length;
            dictionary[word[0]] = long.Parse(word[1]);
        }
    }
    return true;
}

/// <summary>Find best word segmentation for input string.</summary>
/// <param name="input">The string being word segmented.</param>
/// <param name="maxSegmentationWordLength">The maximum word length that should be considered.</param>	
/// <returns>A tuple representing the suggested word segmented text and the sum of logarithmic word occurence probabilities.</returns> 
public static (string segmentedString, decimal probabilityLogSum) WordSegmentationTM(string input, int maxSegmentationWordLength = 20)
{
	(string segmentedString, decimal probabilityLogSum)[] compositions = new(string segmentedString, decimal probabilityLogSum)[input.Length];

	//Triangular Matrix of dimensions n*m : n=input.length; m=Min(input.length,maximum word length) 
	//outer loop: matrix row
	//generate/test all possible part start positions
	for (int j = 0; j < input.Length; j++)
	{
		//position which holds the best segmentation for the prefix, which will be combined with part1
		int prefixIndex = j - 1;
		int remainderLength = input.Length - j;

		//inner loop : matrix column (triangular: loop becomes shorter as remainder becomes shorter)
		//generate/test all possible part lengths: part can't be bigger than longest word in dictionary (other than long unknown word)
		for (int i = 1; i <= Math.Min(remainderLength, maxSegmentationWordLength); i++)
		{
			//Calculate the Naive Bayes probability of a sequence of words (iterative in logarithmic scale)
			string part1 = input.Substring(j, i);
			decimal ProbabilityLogPart1 = 0;
			if (dictionary.TryGetValue(part1, out long wordCount)) ProbabilityLogPart1 = (decimal)Math.Log10((double)wordCount / (double)N);
			//estimation for unknown words
			else ProbabilityLogPart1 = (decimal)Math.Log10(10.0 / (N * Math.Pow(10.0, part1.Length)));

			//position which holds the best segmentation for a string of length x=prefix length + part1.Length (=i)
			int resultIndex = prefixIndex + i;

			//set values in first loop
			if ((j == 0) || (i == maxSegmentationWordLength))
			{
				//segmentedString, probabilityLogSum
				compositions[resultIndex] = (part1, ProbabilityLogPart1);
			}
			//replace values if better probabilityLogSum
			else if (compositions[resultIndex].probabilityLogSum < compositions[prefixIndex].probabilityLogSum + ProbabilityLogPart1)
			{
				//segmentedString, probabilityLogSum
				compositions[resultIndex] = (compositions[prefixIndex].segmentedString + " " + part1, compositions[prefixIndex].probabilityLogSum + ProbabilityLogPart1);
			}
		}
	}
	return compositions[input.Length - 1];
}

public void test()
{
    if (!LoadDictionary(AppDomain.CurrentDomain.BaseDirectory + "../../../frequency_dictionary_en_82_765.txt"))
        Console.WriteLine("file not found");
    else
        Console.WriteLine(WordSegmentationTM("thequickbrownfoxjumpsoverthelazydog", maximumDictionaryWordLength).segmentedString);
}
