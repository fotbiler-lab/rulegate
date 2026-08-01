namespace Fotbiler.RuleGate.Manifest.Loading;

public static class ManifestLoadCodes
{
    public const string EmptyContent =
        "MANIFEST_YAML_EMPTY_CONTENT";

    public const string RootRequired =
        "MANIFEST_YAML_ROOT_REQUIRED";

    public const string InvalidYaml =
        "MANIFEST_YAML_INVALID";

    public const string ContentTooLarge =
        "MANIFEST_CONTENT_TOO_LARGE";

    public const string FileNotFound =
        "MANIFEST_FILE_NOT_FOUND";

    public const string FileReadFailed =
        "MANIFEST_FILE_READ_FAILED";
}
