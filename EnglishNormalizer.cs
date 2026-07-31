using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Q3TTS.Native
{
    public static class EnglishNormalizer
    {
        private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            { "mr.", "mister" },
            { "mrs.", "missis" },
            { "ms.", "miz" },
            { "dr.", "doctor" },
            { "prof.", "professor" },
            { "etc.", "et cetera" },
            { "vs.", "versus" },
            { "st.", "street" },
            { "co.", "company" },
            { "ltd.", "limited" },
            { "inc.", "incorporated" },
            { "ave.", "avenue" },
            { "rd.", "road" },
            { "corp.", "corporation" },
            { "approx.", "approximately" },
            { "dept.", "department" },
            { "govt.", "government" },
            { "jr.", "junior" },
            { "sr.", "senior" },
            { "gen.", "general" },
            { "sgt.", "sergeant" },
            { "capt.", "captain" },
            { "vol.", "volume" },
            { "fig.", "figure" },
            { "no.", "number" },
            { "jan.", "january" },
            { "feb.", "february" },
            { "mar.", "march" },
            { "apr.", "april" },
            { "jun.", "june" },
            { "jul.", "july" },
            { "aug.", "august" },
            { "sep.", "september" },
            { "sept.", "september" },
            { "oct.", "october" },
            { "nov.", "november" },
            { "dec.", "december" }
        };

        private static readonly Dictionary<string, string> Contractions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "can't", "cannot" },
            { "won't", "will not" },
            { "don't", "do not" },
            { "doesn't", "does not" },
            { "didn't", "did not" },
            { "isn't", "is not" },
            { "aren't", "are not" },
            { "wasn't", "was not" },
            { "weren't", "were not" },
            { "haven't", "have not" },
            { "hasn't", "has not" },
            { "hadn't", "had not" },
            { "wouldn't", "would not" },
            { "shouldn't", "should not" },
            { "couldn't", "could not" }
        };

        private static readonly Dictionary<int, string> OrdinalWords = new()
        {
            { 1, "first" }, { 2, "second" }, { 3, "third" }, { 4, "fourth" },
            { 5, "fifth" }, { 6, "sixth" }, { 7, "seventh" }, { 8, "eighth" },
            { 9, "ninth" }, { 10, "tenth" }, { 11, "eleventh" }, { 12, "twelfth" },
            { 13, "thirteenth" }, { 14, "fourteenth" }, { 15, "fifteenth" },
            { 16, "sixteenth" }, { 17, "seventeenth" }, { 18, "eighteenth" },
            { 19, "nineteenth" }
        };

        private static readonly Dictionary<int, string> OrdinalTens = new()
        {
            { 20, "twentieth" }, { 30, "thirtieth" }, { 40, "fortieth" },
            { 50, "fiftieth" }, { 60, "sixtieth" }, { 70, "seventieth" },
            { 80, "eightieth" }, { 90, "ninetieth" }
        };

        private static readonly string[] Ones = { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        private static readonly string[] TensArr = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

        private static readonly Regex UrlRegex = new Regex(@"https?://\S+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex EmailRegex = new Regex(@"\b[\w.+-]+@[\w.-]+\.\w+\b", RegexOptions.Compiled);
        private static readonly Regex OrdinalRegex = new Regex(@"\b(\d+)(st|nd|rd|th)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TimeRegex = new Regex(@"\b(\d{1,2}):(\d{2})(?:\s*(am|pm|a\.m\.|p\.m\.))?\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex YearRegex = new Regex(@"\b(1[0-9]{3}|20[0-9]{2})\b", RegexOptions.Compiled);
        private static readonly Regex FractionRegex = new Regex(@"\b(\d+)/(\d+)\b", RegexOptions.Compiled);
        private static readonly Regex AcronymRegex = new Regex(@"\b([A-Z]{2,6})\b", RegexOptions.Compiled);
        private static readonly Regex DollarRegex = new Regex(@"\$(\d+(?:,\d+)*(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex PoundRegex = new Regex(@"£(\d+(?:,\d+)*(?:\.\d+)?)", RegexOptions.Compiled);
        private static readonly Regex DecimalRegex = new Regex(@"\b\d+(?:,\d+)*\.\d+\b", RegexOptions.Compiled);
        private static readonly Regex IntegerRegex = new Regex(@"\b\d+(?:,\d+)*\b", RegexOptions.Compiled);
        private static readonly Regex PercentRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*%\b", RegexOptions.Compiled);
        private static readonly Regex DegreeFRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*(?:°F|degrees F)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DegreeCRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*(?:°C|degrees C)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SpeedMphRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*mph\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SpeedKmhRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*km/h\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WeightKgRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*kg\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WeightLbsRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*lbs?\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StorageGbRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*GB\b", RegexOptions.Compiled);
        private static readonly Regex StorageTbRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*TB\b", RegexOptions.Compiled);
        private static readonly Regex StorageMbRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*MB\b", RegexOptions.Compiled);
        private static readonly Regex FreqGhzRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*GHz\b", RegexOptions.Compiled);
        private static readonly Regex FreqMhzRegex = new Regex(@"\b(\d+(?:\.\d+)?)\s*MHz\b", RegexOptions.Compiled);

        private static readonly Regex AmpSpacedRegex = new Regex(@"\s+&\s+", RegexOptions.Compiled);
        private static readonly Regex AmpWordRegex = new Regex(@"\b&\b", RegexOptions.Compiled);
        private static readonly Regex MultiSpaceRegex = new Regex(@"[ \t]+", RegexOptions.Compiled);

        private static readonly List<(Regex Regex, string Replacement)> CompiledAbbreviations = new();
        private static readonly List<(Regex Regex, string Replacement)> CompiledContractions = new();
        private static readonly object _dictLock = new();
        private static List<(Regex Regex, string Replacement)>? _userDictEntries;
        private static DateTime _userDictLastWrite = DateTime.MinValue;

        private static readonly HashSet<string> PronounceableAcronyms = new(StringComparer.OrdinalIgnoreCase)
        {
            "NASA", "NATO", "LASER", "RADAR", "RAM", "ROM", "PIN", "SWAT", "GIF", "JPEG", "SaaS", "PaaS", "IaaS", "PUMA", "ASAP"
        };

        private static readonly HashSet<string> CommonEnglishWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "THE", "AND", "FOR", "ARE", "YOU", "THIS", "THAT", "WAS", "WITH", "HAVE", "FROM", "THEY",
            "WILL", "WOULD", "THERE", "THEIR", "WHAT", "SO", "IF", "IN", "ON", "AT", "BY", "TO", "OF",
            "IT", "IS", "AM", "AN", "AS", "BE", "DO", "GO", "HE", "ME", "MY", "NO", "OR", "UP", "WE",
            "US", "DAY", "NEW", "NOW", "OUT", "SEE", "WAY", "WHO", "ITS", "LET", "PUT", "SAY", "SHE",
            "TOO", "USE", "CAN", "HER", "ALL", "ANY", "ONE", "TWO", "HAS", "HIM", "HIS", "HOW", "MAN",
            "BOY", "DID", "GET", "NOT"
        };

        static EnglishNormalizer()
        {
            foreach (var kvp in Abbreviations)
            {
                string pattern = @"\b" + Regex.Escape(kvp.Key);
                CompiledAbbreviations.Add((new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), kvp.Value));
            }
            foreach (var kvp in Contractions)
            {
                string pattern = @"\b" + Regex.Escape(kvp.Key) + @"\b";
                CompiledContractions.Add((new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), kvp.Value));
            }
        }

        public static string Normalize(string text, string baseDir = "")
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // 1. Apply User Dictionary
            string dictPath = !string.IsNullOrEmpty(baseDir)
                ? Path.Combine(baseDir, "user_dict_en.txt")
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user_dict_en.txt");

            text = ApplyUserDictionary(text, dictPath);

            // 2. URLs and Emails
            text = UrlRegex.Replace(text, " website ");
            text = EmailRegex.Replace(text, " email address ");

            // 3. Contractions & Abbreviations
            foreach (var (regex, replacement) in CompiledContractions)
            {
                text = regex.Replace(text, replacement);
            }
            foreach (var (regex, replacement) in CompiledAbbreviations)
            {
                text = regex.Replace(text, replacement);
            }

            // 4. Units & Measurements (Expand before numbers)
            text = PercentRegex.Replace(text, "$1 percent");
            text = DegreeFRegex.Replace(text, "$1 degrees fahrenheit");
            text = DegreeCRegex.Replace(text, "$1 degrees celsius");
            text = SpeedMphRegex.Replace(text, "$1 miles per hour");
            text = SpeedKmhRegex.Replace(text, "$1 kilometers per hour");
            text = WeightKgRegex.Replace(text, "$1 kilograms");
            text = WeightLbsRegex.Replace(text, "$1 pounds");
            text = StorageGbRegex.Replace(text, "$1 gigabytes");
            text = StorageTbRegex.Replace(text, "$1 terabytes");
            text = StorageMbRegex.Replace(text, "$1 megabytes");
            text = FreqGhzRegex.Replace(text, "$1 gigahertz");
            text = FreqMhzRegex.Replace(text, "$1 megahertz");

            // 5. Currency
            text = DollarRegex.Replace(text, match =>
            {
                string raw = match.Groups[1].Value.Replace(",", "");
                if (double.TryParse(raw, out double val))
                {
                    long dollars = (long)Math.Floor(val);
                    int cents = (int)Math.Round((val - dollars) * 100);
                    string res = NumberToWords(dollars) + (dollars == 1 ? " dollar" : " dollars");
                    if (cents > 0)
                    {
                        res += " and " + NumberToWords(cents) + (cents == 1 ? " cent" : " cents");
                    }
                    return res;
                }
                return match.Value;
            });

            text = PoundRegex.Replace(text, match =>
            {
                string raw = match.Groups[1].Value.Replace(",", "");
                if (double.TryParse(raw, out double val))
                {
                    long pounds = (long)Math.Floor(val);
                    int pence = (int)Math.Round((val - pounds) * 100);
                    string res = NumberToWords(pounds) + (pounds == 1 ? " pound" : " pounds");
                    if (pence > 0)
                    {
                        res += " and " + NumberToWords(pence) + (pence == 1 ? " penny" : " pence");
                    }
                    return res;
                }
                return match.Value;
            });

            // 6. Time
            text = TimeRegex.Replace(text, match =>
            {
                int h = int.Parse(match.Groups[1].Value);
                int m = int.Parse(match.Groups[2].Value);
                string ampm = match.Groups[3].Value.ToLower();

                string hStr = NumberToWords(h);
                string mStr = m == 0 ? "o'clock" : (m < 10 ? "oh " + NumberToWords(m) : NumberToWords(m));
                string apStr = string.IsNullOrEmpty(ampm) ? "" : (ampm.StartsWith("a") ? " a m" : " p m");
                return $"{hStr} {mStr}{apStr}";
            });

            // 7. Ordinals
            text = OrdinalRegex.Replace(text, match =>
            {
                if (long.TryParse(match.Groups[1].Value, out long n))
                {
                    return NumberToOrdinalWords(n);
                }
                return match.Value;
            });

            // 8. Fractions
            text = FractionRegex.Replace(text, match =>
            {
                if (int.TryParse(match.Groups[1].Value, out int num) && int.TryParse(match.Groups[2].Value, out int den))
                {
                    if (den == 2) return num == 1 ? "one half" : NumberToWords(num) + " halves";
                    if (den == 4) return num == 1 ? "one quarter" : NumberToWords(num) + " quarters";
                    string denOrd = NumberToOrdinalWords(den);
                    return num == 1 ? $"one {denOrd}" : $"{NumberToWords(num)} {denOrd}s";
                }
                return match.Value;
            });

            // 9. Years
            text = YearRegex.Replace(text, match =>
            {
                int yr = int.Parse(match.Value);
                if (yr >= 1000 && yr <= 1999)
                {
                    int part1 = yr / 100;
                    int part2 = yr % 100;
                    string p2 = part2 == 0 ? "hundred" : (part2 < 10 ? "oh " + NumberToWords(part2) : NumberToWords(part2));
                    return $"{NumberToWords(part1)} {p2}";
                }
                if (yr >= 2000 && yr <= 2099)
                {
                    int part2 = yr % 100;
                    return part2 == 0 ? "two thousand" : $"twenty {NumberToWords(part2)}";
                }
                return match.Value;
            });

            // 10. Decimals
            text = DecimalRegex.Replace(text, match =>
            {
                string raw = match.Value.Replace(",", "");
                string[] parts = raw.Split('.');
                if (parts.Length == 2 && long.TryParse(parts[0], out long whole))
                {
                    StringBuilder decSb = new StringBuilder();
                    decSb.Append(NumberToWords(whole)).Append(" point ");
                    foreach (char c in parts[1])
                    {
                        if (char.IsDigit(c))
                        {
                            decSb.Append(Ones[c - '0']).Append(" ");
                        }
                    }
                    return decSb.ToString().TrimEnd();
                }
                return match.Value;
            });

            // 11. Integers
            text = IntegerRegex.Replace(text, match =>
            {
                string raw = match.Value.Replace(",", "");
                if (long.TryParse(raw, out long val))
                {
                    return NumberToWords(val);
                }
                return match.Value;
            });

            // 12. Ampersand and Acronym spellout
            text = AmpSpacedRegex.Replace(text, " and ");
            text = AmpWordRegex.Replace(text, " and ");

            text = AcronymRegex.Replace(text, match =>
            {
                string acronym = match.Value;
                if (PronounceableAcronyms.Contains(acronym) || CommonEnglishWords.Contains(acronym)) return acronym;

                StringBuilder sb = new StringBuilder();
                foreach (char c in acronym)
                {
                    sb.Append(c).Append(" ");
                }
                return sb.ToString().TrimEnd();
            });

            text = MultiSpaceRegex.Replace(text, " ");
            return text.Trim();
        }

        private static string ApplyUserDictionary(string text, string dictPath)
        {
            if (!File.Exists(dictPath)) return text;

            List<(Regex Regex, string Replacement)> entries;
            lock (_dictLock)
            {
                DateTime lastWrite = File.GetLastWriteTime(dictPath);
                if (_userDictEntries == null || lastWrite > _userDictLastWrite)
                {
                    _userDictEntries = LoadUserDictionary(dictPath);
                    _userDictLastWrite = lastWrite;
                }
                entries = _userDictEntries;
            }

            foreach (var (regex, replacement) in entries)
            {
                text = regex.Replace(text, replacement);
            }
            return text;
        }

        private static List<(Regex Regex, string Replacement)> LoadUserDictionary(string path)
        {
            var list = new List<(Regex Regex, string Replacement)>();
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                    string[] parts = trimmed.Split(',', 2);
                    if (parts.Length == 2)
                    {
                        string word = parts[0].Trim();
                        string read = parts[1].Trim();
                        if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(read))
                        {
                            string pattern = @"\b" + Regex.Escape(word) + @"\b";
                            list.Add((new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), read));
                        }
                    }
                }
            }
            catch { }
            return list;
        }

        public static string NumberToWords(long number)
        {
            if (number == 0) return "zero";
            if (number < 0)
            {
                ulong pos = (ulong)-(number + 1) + 1;
                return "minus " + NumberToWordsUlong(pos);
            }
            return NumberToWordsUlong((ulong)number);
        }

        private static string NumberToWordsUlong(ulong number)
        {
            if (number == 0) return "zero";

            StringBuilder sb = new StringBuilder();

            if ((number / 1_000_000_000_000) > 0)
            {
                sb.Append(NumberToWordsUlong(number / 1_000_000_000_000)).Append(" trillion ");
                number %= 1_000_000_000_000;
            }

            if ((number / 1_000_000_000) > 0)
            {
                sb.Append(NumberToWordsUlong(number / 1_000_000_000)).Append(" billion ");
                number %= 1_000_000_000;
            }

            if ((number / 1_000_000) > 0)
            {
                sb.Append(NumberToWordsUlong(number / 1_000_000)).Append(" million ");
                number %= 1_000_000;
            }

            if ((number / 1000) > 0)
            {
                sb.Append(NumberToWordsUlong(number / 1000)).Append(" thousand ");
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                sb.Append(NumberToWordsUlong(number / 100)).Append(" hundred ");
                number %= 100;
            }

            if (number > 0)
            {
                if (number < 20)
                {
                    sb.Append(Ones[number]);
                }
                else
                {
                    sb.Append(TensArr[number / 10]);
                    if ((number % 10) > 0)
                    {
                        sb.Append("-").Append(Ones[number % 10]);
                    }
                }
            }

            return sb.ToString().Trim();
        }

        public static string NumberToOrdinalWords(long number)
        {
            if (number <= 0) return number.ToString();
            if (number < 20 && OrdinalWords.TryGetValue((int)number, out string? ord)) return ord;
            if (number < 100 && number % 10 == 0 && OrdinalTens.TryGetValue((int)number, out string? tensOrd)) return tensOrd;

            string words = NumberToWords(number);
            string[] parts = words.Split(' ', '-');
            string lastWord = parts[^1];

            if (lastWord == "one") return words[..^3] + "first";
            if (lastWord == "two") return words[..^3] + "second";
            if (lastWord == "three") return words[..^5] + "third";
            if (lastWord == "five") return words[..^4] + "fifth";
            if (lastWord == "eight") return words[..^5] + "eighth";
            if (lastWord == "nine") return words[..^4] + "ninth";
            if (lastWord == "twelve") return words[..^6] + "twelfth";
            if (lastWord.EndsWith("y")) return words[..^1] + "ieth";

            return words + "th";
        }
    }
}
