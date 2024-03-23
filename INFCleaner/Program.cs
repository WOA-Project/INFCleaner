using System.Text.RegularExpressions;

namespace INFCleaner
{
    internal partial class Program
    {
        internal static void Main(string[] args)
        {
            string dir = args[0];
            string[] files = Directory.GetFiles(dir, "*.inf", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                lines = CleanupCommas(lines);
                lines = CleanupEquals(lines);
                lines = CleanupCommas(lines);
                File.WriteAllLines(file, lines, System.Text.Encoding.Unicode);
            }
        }

        [GeneratedRegex(",[ ]+,")]
        private static partial Regex SpaceInBetweenCommasRegex();

        [GeneratedRegex(@"""([^""]*)""")]
        private static partial Regex QuoteBlockRegex();

        private static string[] SplitLineWithComments(string inputLine)
        {
            List<string> result = [];

            int lastSplit = 0;
            foreach (Match match in QuoteBlockRegex().Matches(inputLine).Cast<Match>())
            {
                if (match.Index != 0)
                {
                    string substring = inputLine[lastSplit..match.Index];

                    if (substring.Contains(';'))
                    {
                        string newline = substring.Split(';')[0];
                        if (!string.IsNullOrEmpty(newline))
                        {
                            result.Add(newline);
                        }

                        string leftover = $";{string.Join(";", substring.Split(';').Skip(1))}";

                        if (match.Index < inputLine.Length)
                        {
                            leftover += inputLine[match.Index..];
                        }

                        if (!string.IsNullOrEmpty(leftover))
                        {
                            result.Add(leftover);
                        }

                        return [.. result];
                    }

                    if (!string.IsNullOrEmpty(substring))
                    {
                        result.Add(substring);
                    }
                }

                if (!string.IsNullOrEmpty(match.Value))
                {
                    result.Add(match.Value);
                }

                lastSplit = match.Index + match.Length;
            }

            if (lastSplit < inputLine.Length)
            {
                string substring = inputLine[lastSplit..];

                if (substring.Contains(';'))
                {
                    string newline = substring.Split(';')[0];
                    if (!string.IsNullOrEmpty(newline))
                    {
                        result.Add(newline);
                    }

                    string leftover = $";{string.Join(";", substring.Split(';').Skip(1))}";

                    if (!string.IsNullOrEmpty(leftover))
                    {
                        result.Add(leftover);
                    }

                    return [.. result];
                }

                if (!string.IsNullOrEmpty(substring))
                {
                    result.Add(substring);
                }
            }

            return [.. result];
        }

        private static string[] SplitLineCustomWithComments(string inputLine, string splitElement)
        {
            string[] lineElements = SplitLineWithComments(inputLine);

            List<string> result = [];
            string currentResult = "";

            foreach (string lineElement in lineElements)
            {
                bool isString = lineElement.StartsWith('\"') && lineElement.EndsWith('\"');
                bool isComment = lineElement.StartsWith(';');

                if (!isString && !isComment && lineElement.Contains(splitElement))
                {
                    string[] elements = lineElement.Split(splitElement);
                    currentResult += elements[0];
                    result.Add(currentResult);
                    currentResult = "";

                    for (int i = 1; i < elements.Length - 1; i++)
                    {
                        result.Add(elements[i]);
                    }

                    if (elements.Length - 1 > 0)
                    {
                        currentResult += elements[^1];
                    }
                }
                else
                {
                    currentResult += lineElement;
                }
            }

            result.Add(currentResult);

            return [.. result];
        }

        private static string[] CleanupCommas(string[] lines)
        {
            List<int[]> commaSizes = [];
            int previousSectionIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Trim().StartsWith('[') || string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith(';'))
                {
                    if (commaSizes.Count > 0)
                    {
                        for (int j = previousSectionIndex; j < i; j++)
                        {
                            string[] lineElements = SplitLineCustomWithComments(lines[j], ",").Select(x => x.Trim()).ToArray();

                            if (lineElements.Length > 1)
                            {
                                for (int k = 0; k < lineElements.Length - 1; k++)
                                {
                                    int maxSizeInArray = commaSizes.Where(x => x.Length > k && x.Length > 1).Select(x => x[k]).Max();
                                    int maxSizeInArrayNext = commaSizes.Where(x => x.Length > k + 1 && x.Length > 1).Select(x => x[k + 1]).Max();

                                    lineElements[k] = maxSizeInArrayNext == 0
                                        ? $"{lineElements[k]},".PadRight(maxSizeInArray + 1)
                                        : $"{$"{lineElements[k]},".PadRight(maxSizeInArray + 1)} ";
                                }
                            }

                            lines[j] = string.Join("", lineElements).Trim();
                            while (true)
                            {
                                string newLine = lines[j];
                                foreach (Match match in SpaceInBetweenCommasRegex().Matches(lines[j]).Cast<Match>())
                                {
                                    string el = newLine[match.Index..(match.Index + match.Length)];
                                    el = el.Replace(" ", "").PadRight(el.Length);
                                    newLine = newLine.Remove(match.Index, match.Length).Insert(match.Index, el);
                                }
                                if (newLine == lines[j])
                                {
                                    break;
                                }
                                lines[j] = newLine;
                            }
                        }

                        commaSizes.Clear();
                    }

                    previousSectionIndex = i + 1;
                }
                else
                {
                    int[] commaSplitSizes = SplitLineCustomWithComments(line, ",").Select(x => x.Trim().Length).ToArray();
                    commaSizes.Add(commaSplitSizes);
                }
            }

            if (commaSizes.Count > 0)
            {
                for (int j = previousSectionIndex; j < lines.Length; j++)
                {
                    string[] lineElements = SplitLineCustomWithComments(lines[j], ",").Select(x => x.Trim()).ToArray();

                    if (lineElements.Length > 1)
                    {
                        for (int k = 0; k < lineElements.Length - 1; k++)
                        {
                            int maxSizeInArray = commaSizes.Where(x => x.Length > k && x.Length > 1).Select(x => x[k]).Max();
                            int maxSizeInArrayNext = commaSizes.Where(x => x.Length > k + 1 && x.Length > 1).Select(x => x[k + 1]).Max();

                            lineElements[k] = maxSizeInArrayNext == 0
                                ? $"{lineElements[k]},".PadRight(maxSizeInArray + 1)
                                : $"{$"{lineElements[k]},".PadRight(maxSizeInArray + 1)} ";
                        }
                    }

                    lines[j] = string.Join("", lineElements).Trim();
                    while (true)
                    {
                        string newLine = lines[j];
                        foreach (Match match in SpaceInBetweenCommasRegex().Matches(lines[j]).Cast<Match>())
                        {
                            string el = newLine[match.Index..(match.Index + match.Length)];
                            el = el.Replace(" ", "").PadRight(el.Length);
                            newLine = newLine.Remove(match.Index, match.Length).Insert(match.Index, el);
                        }
                        if (newLine == lines[j])
                        {
                            break;
                        }
                        lines[j] = newLine;
                    }
                }
            }

            return lines;
        }

        private static string[] CleanupEquals(string[] lines)
        {
            List<int[]> commaSizes = [];
            int previousSectionIndex = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (line.Trim().StartsWith('[') || string.IsNullOrEmpty(line.Trim()) || line.Trim().StartsWith(';'))
                {
                    if (commaSizes.Count > 0)
                    {
                        for (int j = previousSectionIndex; j < i; j++)
                        {
                            string[] lineElements = SplitLineCustomWithComments(lines[j], "=").Select(x => x.Trim()).ToArray();

                            if (lineElements.Length > 1)
                            {
                                for (int k = 0; k < lineElements.Length; k++)
                                {
                                    int maxSizeInArray = commaSizes.Where(x => x.Length > k && x.Length > 1).Select(x => x[k]).Max();
                                    lineElements[k] = lineElements[k].PadRight(maxSizeInArray);
                                }
                            }

                            lines[j] = string.Join(" = ", lineElements).Trim();
                        }

                        commaSizes.Clear();
                    }

                    previousSectionIndex = i + 1;
                }
                else
                {
                    int[] commaSplitSizes = SplitLineCustomWithComments(line, "=").Select(x => x.Trim().Length).ToArray();
                    commaSizes.Add(commaSplitSizes);
                }
            }

            if (commaSizes.Count > 0)
            {
                for (int j = previousSectionIndex; j < lines.Length; j++)
                {
                    string[] lineElements = SplitLineCustomWithComments(lines[j], "=").Select(x => x.Trim()).ToArray();

                    if (lineElements.Length > 1)
                    {
                        for (int k = 0; k < lineElements.Length; k++)
                        {
                            int maxSizeInArray = commaSizes.Where(x => x.Length > k && x.Length > 1).Select(x => x[k]).Max();
                            lineElements[k] = lineElements[k].PadRight(maxSizeInArray);
                        }
                    }

                    lines[j] = string.Join(" = ", lineElements).Trim();
                }
            }

            return lines;
        }
    }
}
