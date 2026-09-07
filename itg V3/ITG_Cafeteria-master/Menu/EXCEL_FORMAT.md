# Holcom Cafeteria Menu — Excel Format

This document describes the **official weekly menu Excel format** used in the `Menu/` folder. The import service (`HolcomMenuExcelParser`) is built to match these files exactly.

## Reference files

| File | Week |
|------|------|
| `dalia menu 25 to 29 may.xlsx` | May 25–29 |
| `dalia holcom 1 JUNE to 5 JUNE menu.xlsx` | June 1–5 |
| `Dalia holcom 15 JUNE to19 JUNE menu.xlsx` | June 15–19 |

## Workbook structure

| Rule | Detail |
|------|--------|
| **Worksheet** | Use a sheet named **`Menu`**. If missing, the first sheet is used (with a warning). |
| **Layout** | **Grid / columnar** — one column per weekday, not one row per day. |
| **Columns** | **B → F** = Monday through Friday (column A is typically empty). |
| **Title row** | Row 2 (approx.): `Holcom Cafeteria Menu` with date range, e.g. `JUNE 15Th-JUNE 19TH , 2026` |
| **Day header row** | Next row: `Monday`, `Tuesday`, … `Friday` across columns B–F |
| **Second block** | Lunch section repeats a title + day header row mid-sheet |

## Hierarchy (per column / per day)

Each **column** is parsed top-to-bottom into this tree:

```
Breakfast                    ← meal period (explicit row)
├── Baguette or Kaak         ← category (plain text, no leading dot)
│   ├── Labneh               ← item (starts with ".")
│   ├── Halloum
│   └── ...
├── Croissant & Puff Pastry
│   ├── Thyme croissant
│   └── ...
└── Eggs
    ├── Scrambled eggs
    └── Shakshouka

Lunch                        ← inferred after second "Holcom Cafeteria Menu" title block
├── Cold Sandwich
│   ├── Turkey & cheese baguette
│   └── ...
├── Salads
├── Hot Sandwich
├── Main Dish
├── Dessert
└── Appetizers & Mouajanat
```

### Row types

| Type | How to recognize | Example |
|------|------------------|---------|
| **Title** | Contains `Holcom Cafeteria Menu` | `Holcom Cafeteria Menu JUNE 1Th-JUNE 5TH , 2026` |
| **Day header** | Row with Mon–Fri across columns | `Monday`, `Tuesday`, … |
| **Meal period** | Exact text `Breakfast` | `Breakfast` |
| **Category** | Non-empty text **without** a leading `.` | `Baguette or Kaak`, `Salads` |
| **Item** | Text starting with `.` (optional space) | `. Labneh`, `.Turkey & cheese baguette` |
| **Footer** | Long promotional / legal text | Skipped automatically |

### Notes

- **Empty cells** in a column are skipped (items may differ per day on the same row).
- **Lunch** is not always labeled; it begins after the **second** title block in the sheet.
- Leading dots on items are **removed** when stored in the database.
- Week dates are read from the title row; filenames like `1 JUNE to 5 JUNE` are used as fallback.

## Expected hierarchy in the app

After import, the database stores:

| Level | Example |
|-------|---------|
| 1 — Meal | Breakfast, Lunch |
| 2 — Category | Baguette or Kaak, Cold Sandwich |
| 3 — Item | Labneh, Turkey & cheese baguette |

Additional nesting is supported by the data model, but the Holcom Excel files use these three levels.

## Upload checklist

1. File is `.xlsx` / `.xlsm` / `.xls`
2. Sheet named **`Menu`** contains the current week
3. Columns B–F have Monday–Friday headers
4. Categories have **no** leading dot; items **do**
5. Date range appears in the title row or filename

## Import workflow in the portal

1. **Upload** the Excel file (drag & drop)
2. System **auto-populates** all five days with categories and items
3. **Review** the preview summary and tree (edit any day if needed)
4. **Save Draft** or **Publish** to persist to the database

## Technical parser

- **Library:** ExcelDataReader (required because these files contain embedded images that break ClosedXML)
- **Code:** `ITG_Cafeteria.Server/Services/HolcomMenuExcelParser.cs`
