namespace DrugRegistry.API.Utils;

public static class StringUtils
{
    private static readonly Dictionary<char, string> CyrillicToLatin = new()
    {
        { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "g" }, { 'д', "d" },
        { 'ѓ', "gj" }, { 'е', "e" }, { 'ж', "zh" }, { 'з', "z" }, { 'ѕ', "dz" },
        { 'и', "i" }, { 'ј', "j" }, { 'к', "k" }, { 'л', "l" }, { 'љ', "lj" },
        { 'м', "m" }, { 'н', "n" }, { 'њ', "nj" }, { 'о', "o" }, { 'п', "p" },
        { 'р', "r" }, { 'с', "s" }, { 'т', "t" }, { 'ќ', "kj" }, { 'у', "u" },
        { 'ф', "f" }, { 'х', "h" }, { 'ц', "c" }, { 'ч', "ch" }, { 'џ', "dj" },
        { 'ш', "sh" }, { 'А', "A" }, { 'Б', "B" }, { 'В', "V" }, { 'Г', "G" },
        { 'Д', "D" }, { 'Ѓ', "Gj" }, { 'Е', "E" }, { 'Ж', "Zh" }, { 'З', "Z" },
        { 'Ѕ', "Dz" }, { 'И', "I" }, { 'Ј', "J" }, { 'К', "K" }, { 'Л', "L" },
        { 'Љ', "Lj" }, { 'М', "M" }, { 'Н', "N" }, { 'Њ', "Nj" }, { 'О', "O" },
        { 'П', "P" }, { 'Р', "R" }, { 'С', "S" }, { 'Т', "T" }, { 'Ќ', "Kj" },
        { 'У', "U" }, { 'Ф', "F" }, { 'Х', "H" }, { 'Ц', "C" }, { 'Ч', "Ch" },
        { 'Џ', "Dj" }, { 'Ш', "Sh" }
    };

    /// <param name="input">The Cyrillic string to convert.</param>
    extension(string input)
    {
        /// <summary>
        ///     Converts a Macedonian Cyrillic string to Latin characters. The conversion is based on a predefined mapping
        ///     of Cyrillic characters to their corresponding Latin equivalents.
        /// </summary>
        /// <returns>The converted string with Latin characters.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the input string is null.</exception>
        /// <remarks>
        ///     The Cyrillic characters and their corresponding Latin equivalents are defined in a static dictionary.
        ///     If a Cyrillic character is not present in the dictionary, it will be copied to the output
        ///     string as-is.
        /// </remarks>
        private string ToLatin()
        {
            var convertedList = input.Select(c => CyrillicToLatin.TryGetValue(c, out var latin) ? latin : c.ToString());
            return string.Join(string.Empty, convertedList);
        }

        /// <summary>
        ///     Converts a Macedonian Cyrillic string to its Latin representation and then returns the upper-cased version of the
        ///     result.
        /// </summary>
        /// <returns>The upper-cased version of the Latin representation of the input string.</returns>
        public string ToUpperLatin()
        {
            return input.ToLatin().ToUpperInvariant();
        }
    }
}