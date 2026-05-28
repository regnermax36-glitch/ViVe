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
        // Curated IDs for the transformation
        private static readonly uint[] CoreOsIds = { 38078204, 44156104, 25350390, 24674586 };
        private static readonly uint[] NextGenAiIds = { 44156104, 48433719, 56031573, 51207788, 54792954, 55345819 };
        private static readonly uint[] Windows12Ids = { 40729830, 44470355, 39281392, 41655236, 42105254, 47205210, 44774629, 44850061 };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            var finalIds = new HashSet<uint>();
            bool processed = false;

            foreach (var arg in args)
            {
                var command = arg.ToLowerInvariant().TrimStart('/');
                switch (command)
                {
                    case "coreos":
                        foreach (var id in CoreOsIds) finalIds.Add(id);
                        // Add keyword discovery for extra coverage
                        foreach (var id in FindFeatureIds(new[] { "WCOS", "CoreOS" })) finalIds.Add(id);
                        processed = true;
                        break;
                    case "nextgenai":
                        foreach (var id in NextGenAiIds) finalIds.Add(id);
                        foreach (var id in FindFeatureIds(new[] { "NextGen", "AI", "Copilot", "Muse", "Generative" })) finalIds.Add(id);
                        processed = true;
                        break;
                    case "windows12":
                        foreach (var id in Windows12Ids) finalIds.Add(id);
                        foreach (var id in FindFeatureIds(new[] { "Germanium", "MTestUx", "Win12", "W12", "Floating" })) finalIds.Add(id);
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
                ApplyFinalFeatures(finalIds.ToArray());
            }
            else
            {
                Console.WriteLine("No valid transformation commands recognized.");
                PrintHelp();
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("ViVeTool - Windows 11 Transformer");
            Console.WriteLine("Usage: ViVeTool /coreos | /nextgenai | /windows12");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine("  Recoded to fully reshape Windows 11 to look and feel like future iterations.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  /coreos      - Apply CoreOS/WCOS environment features");
            Console.WriteLine("  /nextgenai   - Enable NextGen AI and Copilot capabilities");
            Console.WriteLine("  /windows12   - Full UI transformation (Floating Taskbar, New Start, etc.)");
        }

        static void ApplyFinalFeatures(uint[] ids)
        {
            if (ids.Length == 0)
            {
                Console.WriteLine("No transformation features identified.");
                return;
            }

            Console.WriteLine($"Initiating transformation with {ids.Length} feature modifications...");

            var updates = ids.Select(id => new RTL_FEATURE_CONFIGURATION_UPDATE
            {
                FeatureId = id,
                EnabledState = RTL_FEATURE_ENABLED_STATE.Enabled,
                Priority = RTL_FEATURE_CONFIGURATION_PRIORITY.User,
                Operation = RTL_FEATURE_CONFIGURATION_OPERATION.FeatureState
            }).ToArray();

            try
            {
                // Apply to Runtime (Instant effect where possible)
                int runtimeResult = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Runtime);
                Console.WriteLine($"Runtime Update: {(runtimeResult == 0 ? "Success" : $"Error 0x{runtimeResult:X8}")}");

                // Apply to Boot (Persistent effect)
                int bootResult = FeatureManager.SetFeatureConfigurations(updates, RTL_FEATURE_CONFIGURATION_TYPE.Boot);
                Console.WriteLine($"Boot Persistence: {(bootResult == 0 ? "Success" : $"Error 0x{bootResult:X8}")}");

                if (bootResult == 0)
                {
                    FeatureManager.SetBootFeatureConfigurationState(BSD_FEATURE_CONFIGURATION_STATE.BootPending);
                    Console.WriteLine("\nTransformation sequence complete. REBOOT REQUIRED for full effect.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical failure during transformation: {ex.Message}");
            }
        }

        static List<uint> FindFeatureIds(string[] keywords)
        {
            var results = new List<uint>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("FeatureDictionary.pfs"));

            if (resourceName == null) return results;

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
                        if (keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        {
                            results.Add(id);
                        }
                    }
                }
            }
            return results;
        }
    }
}
