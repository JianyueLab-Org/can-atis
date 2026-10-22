using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace CanAtis.Io;

/// <summary>Extracts the embedded, self-contained Python voice worker.</summary>
public static class BundledVoiceWorker
{
    /// <summary>Gets the extracted worker executable, or null for development builds.</summary>
    public static string? GetPath()
    {
        using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("CanAtis.VoiceWorker.exe");
        if (source == null)
            return null;
        var hash = Convert.ToHexString(SHA256.HashData(source));
        var directory = Path.Combine(PathProvider.AppDataFolderPath, "Runtime", hash);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "can-voice-worker.exe");
        if (!File.Exists(path))
        {
            source.Position = 0;
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            using (var destination = File.Create(temporary))
                source.CopyTo(destination);
            File.Move(temporary, path, true);
        }
        return path;
    }
}
