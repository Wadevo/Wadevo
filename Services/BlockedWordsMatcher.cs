namespace Wadevo.Services;

using System.Text.RegularExpressions;

public static class BlockedWordsMatcher
{
    public static bool ContainsBlockedWord(string message, IEnumerable<string> blockedWords)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        foreach (string word in blockedWords)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                continue;
            }

            string pattern = $@"\b{Regex.Escape(word.Trim())}\b";

            if (Regex.IsMatch(message, pattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
