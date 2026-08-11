using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ConnectionSettingsRando
{
    internal enum OptOutAction
    {
        Exclude,
        Include,
        ForceTrue,
        ForceFalse
    }

    internal class OptOutRule(string pattern, Regex regex, OptOutAction action)
    {
        public readonly string Pattern = pattern;
        public readonly Regex Regex = regex;
        public readonly OptOutAction Action = action;
        public bool IsExact => !Pattern.Contains("*");
    }

    internal class OptOutManager
    {
        public List<OptOutRule> Rules = [];

        public void Load()
        {
            Rules.Clear();
            string directory = Path.Combine(ConnectionSettingsRando.ModDirectory, "Rules");
            string disabled = Path.Combine(ConnectionSettingsRando.ModDirectory, "Rules", "Disabled");
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!Directory.Exists(disabled))
            {
                Directory.CreateDirectory(disabled);
            }
            string[] paths = Directory.GetFiles(directory, "*.txt");
            ConnectionSettingsRando.Instance.Log($"Loading {paths.Length} opt-out rules from {directory}");
            if (paths.Length == 0)
            {
                string defaultPath = Path.Combine(directory, "Readme.txt");
                File.Create(defaultPath).Dispose();
                LoadReadme(defaultPath);
                return;
            }
            else
            {
                foreach (string path in paths)
                {
                    LoadFile(path);
                }
            }
        }

        private void LoadReadme(string path)
        {
            File.WriteAllText(
                path,
                """
                # Connection Settings Randomizer Rules
                #
                # Rules control whether individual settings are randomized and,
                # for boolean settings, whether they are forced to true or false.
                #
                # Supported actions:
                #
                # Exclude:
                #   Prevents the setting from being randomized.
                #
                # Include:
                #   Allows the setting to be randomized, overriding an applicable
                #   Exclude rule. Useful for exceptions to wildcard exclusions.
                #
                # ForceTrue:
                #   Forces a boolean setting to true.
                #
                # ForceFalse:
                #   Forces a boolean setting to false.
                #
                # Rules use the member's full path.
                #
                # Exact matches take priority over wildcard matches.
                # If multiple rules have the same specificity, the last match wins.
                #
                # Wildcards are supported using '*'.
                #
                # Example: Exclude everything under Settings, except SomeSetting:
                #
                # Exclude:
                # MyConnection.Settings.*
                #
                # Include:
                # MyConnection.Settings.SomeSetting
                #
                # Example: Force a specific boolean:
                #
                # ForceTrue:
                # MyConnection.Settings.EnableSomething
                #
                # ForceFalse:
                # MyConnection.Settings.DisableSomething
                #
                # This file contains no active rules by default.
                """);
        }
        private void LoadFile(string path)
        {
            ConnectionSettingsRando.Instance.Log($"Loading rules file: {Path.GetFileName(path)}");
            OptOutAction? currentAction = null;
            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) ||
                    line.StartsWith("#"))
                    continue;

                if (TryParseSection(
                    line,
                    out OptOutAction action))
                {
                    currentAction = action;
                    continue;
                }

                if (currentAction == null)
                    continue;

                AddRule(line, currentAction.Value);
            }
        }
        public OptOutRule GetRule(string pattern)
        {
            OptOutRule match = null;

            foreach (OptOutRule rule in Rules)
            {
                if (!rule.Regex.IsMatch(pattern))
                    continue;
                ConnectionSettingsRando.Instance.LogDebug(
                    $"Rule candidate for '{pattern}': " +
                    $"[{rule.Action}] {rule.Pattern}");
                // Always prefer an exact match over a wildcard.
                if (match is not null &&
                    match.IsExact &&
                    !rule.IsExact)
                {
                    continue;
                }
                match = rule;
            }

            if (match is not null)
            {
                ConnectionSettingsRando.Instance.LogDebug(
                    $"Rule selected for '{pattern}': " +
                    $"[{match.Action}] {match.Pattern}");
            }

            return match;
        }

        private void AddRule(
            string pattern,
            OptOutAction action)
        {
            Regex regex = CreateRegex(pattern);
            Rules.Add(new OptOutRule(pattern, regex, action));
        }

        private static Regex CreateRegex(string pattern)
        {
            string regexPattern =
                "^" +
                Regex.Escape(pattern)
                    .Replace("\\*", ".*") +
                "$";

            return new Regex(
                regexPattern,
                RegexOptions.Compiled);
        }

        private static bool TryParseSection(
            string line,
            out OptOutAction action)
        {
            action = default;

            switch (line.ToLowerInvariant())
            {
                case "exclude:":
                    action = OptOutAction.Exclude;
                    return true;
                
                case "include:":
                    action = OptOutAction.Include;
                    return true;

                case "forcetrue:":
                    action = OptOutAction.ForceTrue;
                    return true;

                case "forcefalse:":
                    action = OptOutAction.ForceFalse;
                    return true;

                default:
                    return false;
            }
        }
    }
}