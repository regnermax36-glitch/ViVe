using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Albacore.ViVe;
using Albacore.ViVe.NativeEnums;
using Albacore.ViVe.NativeStructs;

namespace Albacore.ViVeTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var allKeywords = new List<string>();
            bool processed = false;

            foreach (var arg in args)
            {
                var command = arg.ToLowerInvariant().TrimStart('/');
                switch (command)
                {
                    case "coreos":
                        allKeywords.AddRange(new[] { "WCOS", "CoreOS" });
                        processed = true;
                        break;
                    case "nextgenai":
                        allKeywords.AddRange(new[] { "NextGen", "AI", "Copilot", "Muse", "Generative", "StudioEffects" });
                        processed = true;
                        break;
                    case "windows12":
                        allKeywords.AddRange(new[] { "Germanium", "MTestUx", "Win12", "W12", "Floating" });
                        processed = true;
                        break;
                    case "?":
                    case "help":
                        PrintHelp();
                        return;
                }
            }

            if (processed)
            {
                ApplyFeatures(allKeywords.Distinct().ToArray());
            }
            else
            {
                Console.WriteLine("No valid commands recognized.");
                PrintHelp();
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("ViVeTool - Modernized Feature Discovery Tool");
            Console.WriteLine("Usage: ViVeTool <command1> [command2] ...");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  /coreos      - Enable CoreOS features");
            Console.WriteLine("  /nextgenai   - Enable NextGen AI features");
            Console.WriteLine("  /windows12   - Enable Windows 12 (Germanium) features");
        }

        static void ApplyFeatures(string[] keywords)
        {
            Console.WriteLine($"Searching for features related to: {string.Join(", ", keywords)}...");
            var ids = FindFeatureIds(keywords);
            if (ids.Count == 0)
            {
                Console.WriteLine("No features found for these keywords.");
                return;
            }

            Console.WriteLine($"Found {ids.Count} features. Applying to Runtime and Boot stores...");

            var updates = ids.Select(id => new RTL_FEATURE_CONFIGURATION_UPDATE
            {
                FeatureId = id,
                EnabledState = RTL_FEATURE_ENABLED_STATE.Enabled,
                Priority = RTL_FEATURE_CONFIGURATION_PRIORITY.User,
                Operation = RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState
            }).ToArray();

            try
            {
                int runtimeResult = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
                Console.WriteLine($"Runtime store: {(runtimeResult == 0 ? "Success" : $"Failed (0x{runtimeResult:X8})")}");

                int bootResult = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Boot);
                Console.WriteLine($"Boot store: {(bootResult == 0 ? "Success" : $"Failed (0x{bootResult:X8})")}");

                if (bootResult == 0)
                {
                    FeatureManager.SetBootFeatureConfigurationState(BSD_FEATURE_CONFIGURATION_STATE.BootPending);
                    Console.WriteLine("A reboot is recommended to apply boot-persistent changes.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying features: {ex.Message}");
            }
        }

        static List<uint> FindFeatureIds(string[] keywords)
        {
            var results = new HashSet<uint>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("FeatureDictionary.pfs"));

            if (resourceName == null)
            {
                Console.WriteLine("Error: Feature dictionary not found in resources.");
                return new List<uint>();
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    var name = parts[0];
                    if (uint.TryParse(parts[1], out uint id))
                    {
                        foreach (var keyword in keywords)
                        {
                            if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                results.Add(id);
                                break;
                            }
                        }
                    }
                }
            }
            return results.ToList();
        }
    }
}
