namespace Evolve.Creatures
{
    /// <summary>
    /// One key for a species, whatever way its name is written: "Thằn Lằn Bờ Hồ", the prefab name "ThanLanBoHo", "ThanLanBoHo(Clone)" and "ThanLanBoHo (1)" all
    /// give "thanlanboho". The cave team names prefabs in PascalCase without accents, the game data uses the display names.
    /// </summary>
    public static class SpeciesNames
    {
        static readonly string[] Letters =
        {
            "a", "àáảãạăằắẳẵặâầấẩẫậ", "e", "èéẻẽẹêềếểễệ", "i", "ìíỉĩị", "o", "òóỏõọôồốổỗộơờớởỡợ", "u", "ùúủũụưừứửữự", "y", "ỳýỷỹỵ", "d", "đ",
        };

        public static string Key(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int paren = name.IndexOf('(');
            if (paren >= 0) name = name.Substring(0, paren);   // "(Clone)", "(1)"
            var sb = new System.Text.StringBuilder(name.Length);
            foreach (char raw in name.ToLowerInvariant())
            {
                char c = raw;
                for (int i = 0; i < Letters.Length; i += 2)
                    if (Letters[i + 1].IndexOf(c) >= 0) { c = Letters[i][0]; break; }
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
