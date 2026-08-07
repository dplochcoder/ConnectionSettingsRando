# ConnectionSettingsRando (CSR)

ConnectionSettingsRando was designed to randomize settings from Hollow Knight Randomizer connections.

The goal is to allow individual connections to expose their settings to CSR, which can then apply a shared randomization algorithm before generation begins.

## Current Status

CSR is a proof of concept extension of what RandoSettingsRandomizer provides. The current implementation has pretty restricted options at the moment.

Supported:
- Registering settings objects from external connections
- Boolean settings
- Numeric settings
- Enum settings
- Nested settings objects
- Fields and properties
- Individual connection opt-in/out
- Boolean enabling weighting odds.

Not currently supported:
- Attribute-based constraints (IE: `DynamicBound`)
- Custom randomization rules per connection.
- Dependency-aware randomization between settings.

## Architecture

CSR works by having connections register their settings objects.

A connection provides:
- A name
- A getter for its settings
- An override callback to apply randomized settings

Example:

```csharp
CSR.Register(
    AccessRandomizer.Instance.GetName(),
    () => AccessManager.Settings,
    s => SettingsRandomizer.CopyTo(s, AccessManager.Settings)
);
```

CSR does not need to know about individual connection settings classes. It only operates on the object it receives.

## Randomization Flow

The current lifecycle is:

```
RandomizerMod starts generation

        |
        v

RandoController.OnBeginRun

        |
        v

CSR.RandomizeAll(rng)

        |
        v

Registered connections receive randomized settings

        |
        v

RandomizerCore generation begins
```

CSR uses the same RNG provided by RandomizerMod, ensuring:

```
Same seed
+ Same connection settings
+ Same CSR version

= Same randomized result
```

## Nested Settings

Nested settings objects are supported automatically.

Example:

```csharp
public class AccessSettings
{
    public bool SplitTram { get; set; }

    public CustomKeySettings CustomKeys { get; set; } = new();
}

public class CustomKeySettings
{
    public bool MapperKey;
    public bool SlyKey;
}
```

CSR will recurse into `CustomKeys` and randomize its members as well.

## Attributes

Currently, CSR avoids randomizing any member with attributes attached.

Example:

```csharp
[MenuRange(0, 10)]
public int Cost { get; set; }
```

will retain its existing value.

This is intentional while constraint handling is being designed.

Future versions will support:
- `DynamicBound`
- Other MenuChanger attributes

## Connection Integration

A connection should:

1. Define its normal settings object.
2. Register it with CSR during initialization.
3. Allow CSR to override settings before generation begins.

## Planned Features

- Attribute constraint system
- Dynamic bounds
- Custom randomization handlers
