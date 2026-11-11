using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace RutaViva.IntegrationContracts.Tests;

internal static class ContractSnapshot
{
    private const string SnapshotsFolder = "Snapshots";
    private const string SnapshotExtension = ".approved.json";
    private const string CreatedMessage = "Snapshot '{0}' did not exist and was created. Review it and commit it, then run the test again.";
    private const string MismatchMessage = "The contract of '{0}' no longer matches its approved snapshot. If the change is intentional, review it and replace the .approved.json file.";

    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);

    public static void Verify(string eventName, string actualJson)
    {
        string path = SnapshotPath(eventName);

        if (!File.Exists(path))
        {
            CreateAndFail(path, actualJson);
        }

        JsonNode? approved = JsonNode.Parse(File.ReadAllText(path));
        JsonNode? actual = JsonNode.Parse(actualJson);

        JsonNode.DeepEquals(approved, actual).Should().BeTrue(MismatchMessage, eventName);
    }

    private static void CreateAndFail(string path, string actualJson)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, actualJson + Environment.NewLine);
        Assert.Fail(string.Format(CreatedMessage, path));
    }

    private static string SnapshotPath(string eventName) =>
        Path.Combine(ProjectDirectory(), SnapshotsFolder, eventName + SnapshotExtension);

    private static string ProjectDirectory([CallerFilePath] string sourcePath = "") => Path.GetDirectoryName(sourcePath)!;

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
