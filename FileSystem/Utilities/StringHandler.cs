namespace FileSystem.Utilities
{
    public static class StringHandler
    {
        public static string[] SplitString(string str, char separator)
        {
            if (str == null)
                return Array.Empty<string>();

            int count = 1;
            for (int i = 0; i < str.Length; i++)
                if (str[i] == separator)
                    count++;
            

            string[] result = new string[count];
            int resultIndex = 0;
            int startIndex = 0;

            for (int i = 0; i < str.Length; i++)
                if (str[i] == separator)
                {
                    result[resultIndex] = CreateWord(str, startIndex, i - startIndex);
                    resultIndex++;
                    startIndex = i + 1;
                }

            result[resultIndex] = CreateWord(str, startIndex, str.Length - startIndex);

            return result;
        }

        private static string CreateWord(string source, int start, int length)
        {
            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = source[start + i];
            }
            return new string(chars);
        }

        public static short Compare(string str1, string str2)
        {
            if (str1 == null && str2 == null)
                return 0;
            if (str1 == null)
                return -1;
            if (str2 == null)
                return 1;

            int minLength = str1.Length < str2.Length ? str1.Length : str2.Length;
            for (int i = 0; i < minLength; i++)
            {
                if (str1[i] < str2[i])
                    return -1;
                if (str1[i] > str2[i])
                    return 1;
            }

            if (str1.Length < str2.Length)
                return -1;
            if (str1.Length > str2.Length)
                return 1;

            return 0;
        }

        public static (string, string) SplitByLastOccurrence(string input, char separator)
        {
            if (string.IsNullOrEmpty(input))
                return (string.Empty, string.Empty);

            int lastIndex = input.LastIndexOf(separator);

            if (lastIndex == -1)
                return (input, string.Empty);

            string firstPart = input.Substring(0, lastIndex);
            string secondPart = input.Substring(lastIndex + 1);

            return (firstPart, secondPart);
        }
    }
}
