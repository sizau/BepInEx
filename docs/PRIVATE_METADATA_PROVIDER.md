# Private metadata provider

## Provider contract

Build a private assembly and deploy it to one of:

- `BepInEx/core`
- `BepInEx/patchers`

Default discovered names are:

- `Metadata.Provider.dll`

You can override or extend discovered names via environment variable `BEPINEX_METADATA_PROVIDER_NAMES`.

Expose one of the following static methods (public):

- `string ProvideMetadata(string outputPath, string referencePath)`
- `string ProvideMetadata(string outputPath)`

Return value must be a valid on-disk metadata file path.

## Notes

- The provider is discovered and loaded dynamically by IL2CPP fallback path.
- Placing the provider under `patchers` is supported as a file location, but it does not need to implement `BasePatcher`.
- Recommended: keep provider in a private repository and publish binary artifact only.
