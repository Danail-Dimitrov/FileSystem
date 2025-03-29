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
    }
}
