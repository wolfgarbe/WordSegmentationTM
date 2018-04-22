using System;

class Program
{
    static void Main(string[] args)
    {
        WordSegmentationTM wordSegmentation = new WordSegmentationTM();

        if (!wordSegmentation.LoadDictionary(AppDomain.CurrentDomain.BaseDirectory + "frequency_dictionary_en_82_765.txt"))
            Console.WriteLine("Dictionary file not found.");
        else
        {
            string test = "thequickbrownfoxjumpsoverthelazydog";
            Console.WriteLine("Input : " + test);
            Console.WriteLine("Output: "+wordSegmentation.Segment(test).segmentedString);
        }

        Console.ReadKey();
    }
}

