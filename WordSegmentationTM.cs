//MIT License: Copyright (c) 2018 Wolf Garbe
//https://github.com/wolfgarbe/WordSegmentationTM
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

Console.WriteLine(WordSegmentationTM("thequickbrownfoxjumpsoverthelazydog", maximumDictionaryWordLength).segmentedString);