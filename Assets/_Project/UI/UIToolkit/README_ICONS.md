# Global Icons Setup

This project now supports a centralized icon workflow for UI item/action icons.

## 1) Create the catalog asset

Create a `GlobalIconCatalog` asset from:

- `Create > Sporae > UI > Global Icon Catalog`

Then place it in:

- `Assets/_Project/Resources/UI/GlobalIconCatalog.asset`

The resolver auto-loads this exact resource path.

## 2) Fill icon mappings in Inspector

`GlobalIconCatalog` contains:

- **Overrides by Item TypeId** (`typeId -> icon`)
- **Overrides by Category Key** (`category -> icon`)
- **Overrides by Action Key** (`action -> icon`)
- **Overrides by PlantCode** (`plantCode -> icon`)

### Category keys (built-in)

- `plant`
- `seed`
- `spore`
- `fruit`
- `water`
- `fertilizer`
- `additive`
- `reagent`
- `stemcell`
- `protein`
- `food`
- `preseed`
- `misc`
- `action`

### Action keys

Actions are matched by normalized key (lowercase alphanumeric only).
Example:

- `Overwatering` -> `overwatering`
- `Blue LED x2` -> `blueledx2`

## 3) Where it is used

- Notifications item icons
- Player inventory rows
- Seed inventory rows
- TopBar pH tooltip "Active Modifiers" icons

If an icon is not found, the resolver falls back to defaults.
