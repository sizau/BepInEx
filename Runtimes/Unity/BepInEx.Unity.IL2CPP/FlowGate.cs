using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BepInEx.Unity.IL2CPP;

internal static class FlowGate
{
    private const string PROVIDER_NAMES_ENV_VAR = "BEPINEX_METADATA_PROVIDER_NAMES";

    private static readonly string[] DefaultCarrierNames =
    {
        "Metadata.Provider"
    };

    private const string ENTRY_POINT_NAME = "ProvideMetadata";

    public static bool TryRun(string outputPath, string referencePath, out string dumpedPath)
    {
        dumpedPath = null;

        foreach (var carrierPath in EnumerateCarriers())
        {
            try
            {
                var assembly = Assembly.LoadFrom(carrierPath);
                if (!TryResolveEntryPoint(assembly, out var method, out var oneArgMode))
                    continue;

                var result = oneArgMode
                    ? method.Invoke(null, new object[] { outputPath })
                    : method.Invoke(null, new object[] { outputPath, referencePath });

                if (result is not string path || string.IsNullOrWhiteSpace(path))
                    continue;

                if (!File.Exists(path))
                    continue;

                dumpedPath = path;
                return true;
            }
            catch
            {
                // Intentionally ignore to avoid surfacing details.
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateCarriers()
    {
        var roots = new[]
        {
            Paths.BepInExAssemblyDirectory,
            Paths.PatcherPluginPath
        };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root))
                continue;

            foreach (var name in EnumerateCarrierNames())
            {
                var candidate = Path.Combine(root, name + ".dll");
                if (File.Exists(candidate) && seen.Add(candidate))
                    yield return candidate;
            }
        }
    }

    private static IEnumerable<string> EnumerateCarrierNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var defaultName in DefaultCarrierNames)
            if (!string.IsNullOrWhiteSpace(defaultName))
                names.Add(defaultName);

        var configuredNames = Environment.GetEnvironmentVariable(PROVIDER_NAMES_ENV_VAR);
        if (!string.IsNullOrWhiteSpace(configuredNames))
        {
            foreach (var raw in configuredNames.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var name = raw.Trim();
                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    name = Path.GetFileNameWithoutExtension(name);

                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
        }

        return names;
    }

    private static bool TryResolveEntryPoint(Assembly assembly, out MethodInfo method, out bool oneArgMode)
    {
        method = null;
        oneArgMode = false;

        foreach (var type in assembly.GetTypes())
        {
            var twoArg = type.GetMethod(ENTRY_POINT_NAME,
                                        BindingFlags.Public | BindingFlags.Static,
                                        null,
                                        new[] { typeof(string), typeof(string) },
                                        null);
            if (IsValidEntryPoint(twoArg))
            {
                method = twoArg;
                return true;
            }

            var oneArg = type.GetMethod(ENTRY_POINT_NAME,
                                        BindingFlags.Public | BindingFlags.Static,
                                        null,
                                        new[] { typeof(string) },
                                        null);
            if (IsValidEntryPoint(oneArg))
            {
                method = oneArg;
                oneArgMode = true;
                return true;
            }
        }

        return false;
    }

    private static bool IsValidEntryPoint(MethodInfo method)
        => method != null && method.ReturnType == typeof(string);
}
