# UI Toolkit Font Workflow

This project uses a global runtime theme:
- `Assets/_Project/UI/UIToolkit/SporaeRuntimeTheme.tss`

Global default font is currently `GNF TTF`.

## Quick Rules

- Leave text elements without local font override -> they inherit global GNF.
- Override only where needed.
- Avoid hardcoded runtime font swaps in scripts unless strictly required.

## Override a single element (UI Builder)

For one `Label` / `Button` / `TextField`, set style:

`-unity-font-definition: url("project://database/Assets/<your-font>.ttf?...");`

You can also set `-unity-font` with the same URL for compatibility.

## Override via class (recommended)

1. Add a class in a USS/TSS file:

```css
.sp-font-my-custom {
  -unity-font-definition: url("project://database/Assets/<your-font>.ttf?...#<FontName>");
  -unity-font: url("project://database/Assets/<your-font>.ttf?...#<FontName>");
}
```

2. Assign class `sp-font-my-custom` to any text element.

This keeps overrides clean and reusable without touching C#.

## Override from Inspector (scripted panels)

Some controllers expose optional `Font` fields in Inspector.
If the field is empty, the element inherits the global theme font.

Current example:
- `PlantCardV3TerminalController._consoleMonoFont`

## Troubleshooting

- If a font change is not visible, check that no inline UXML style or C# code is overriding the element.
- Reimport the modified USS/TSS and re-enter Play mode.
