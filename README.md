# xeokit-metadata

The `metadata` iscommand line tool for extracting
the structural hierarchy of the building elements within an `IFC` into the
[metadata format of the `xeokit-sdk`][0].

## Usage

Run the command:

```
~ metadata input.ifc output.json
```

## Credits

Based on the version from [BIMspot][1], streams the json directly to disk using the `Utf8JsonWriter`

[0]: https://github.com/xeokit/xeokit-sdk/wiki/Viewing-BIM-Models-Offline
[1]: hhttps://github.com/bimspot/xeokit-metadata/