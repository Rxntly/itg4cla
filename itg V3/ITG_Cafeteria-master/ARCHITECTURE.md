# ITG Cafeteria Menu System — Architecture

## Overview

ASP.NET Core 8 Web API + React 19 SPA for managing and displaying weekly cafeteria menus (Monday–Friday) with unlimited hierarchical nesting, Excel import, JWT authentication for staff, and a public menu viewer.

## Data Model (Adjacency List)

The menu hierarchy uses a **self-referencing adjacency list** so any depth is supported without schema changes.

```
WeeklyMenu (1) ──< MenuDay (5 per week, Mon–Fri) ──< MenuNode (tree, unlimited depth)
CafeteriaUser (1) ──< WeeklyMenu
```

| Entity | Purpose |
|--------|---------|
| `CafeteriaUser` | Staff login (email + BCrypt password hash) |
| `WeeklyMenu` | One record per week (WeekStartDate = Monday), Draft or Published |
| `MenuDay` | One per weekday (Mon–Fri) within a weekly menu |
| `MenuNode` | Tree node: `ParentId` nullable for roots, `Label`, `SortOrder` |

### Why adjacency list?

- Unlimited nesting (Breakfast → Baguette → Cheese Baguette → …)
- Different tree shapes per day
- No migrations when menu structure changes
- Simple CRUD and Excel import mapping

## Backend Structure

```
ITG_Cafeteria.Server/
├── Controllers/
│   ├── AuthController.cs          POST /api/auth/login
│   ├── MenusController.cs         CRUD + publish (authorized)
│   ├── ImportController.cs        Excel preview + save (authorized)
│   └── PublicMenuController.cs    Public read endpoints
├── Data/
│   └── CafeteriaDbContext.cs
├── Models/
│   ├── Entities/
│   ├── DTOs/
│   └── Enums/
├── Services/
│   ├── AuthService.cs             JWT generation
│   ├── MenuService.cs             Menu CRUD, tree sync, public queries
│   ├── ExcelImportService.cs      ClosedXML parser
│   ├── MenuMapper.cs              Entity ↔ DTO tree mapping
│   ├── WeekHelper.cs              ISO week start (Monday)
│   └── DbSeeder.cs                Migrations + default user
└── Configuration/
    └── AppSettings.cs             JwtSettings, SeedSettings
```

## API Endpoints

### Authentication
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/login` | Staff login, returns JWT |

### Staff (requires `Authorization: Bearer <token>`)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/menus` | List all weekly menus |
| GET | `/api/menus/{id}` | Get menu by ID |
| GET | `/api/menus/week/{weekStart}` | Get menu by week (yyyy-MM-dd) |
| POST | `/api/menus` | Create / save menu |
| PUT | `/api/menus/{id}` | Update menu |
| POST | `/api/menus/{id}/publish` | Publish menu |
| DELETE | `/api/menus/{id}` | Delete menu |
| POST | `/api/import/preview` | Upload Excel, returns preview |
| POST | `/api/import/save` | Save imported (optionally publish) |

### Public (no auth)
| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/public/today` | Today's published menu |
| GET | `/api/public/date/{date}` | Menu for a specific date |
| GET | `/api/public/week/{weekStart}` | Full published week |
| GET | `/api/public/weeks` | List published weeks |

## Excel Import Format

The parser targets the **Holcom cafeteria grid format** used in the `Menu/` reference files. Full specification: [`Menu/EXCEL_FORMAT.md`](Menu/EXCEL_FORMAT.md).

### Layout (columnar grid)

| Column | Day |
|--------|-----|
| B | Monday |
| C | Tuesday |
| D | Wednesday |
| E | Thursday |
| F | Friday |

- Worksheet name: **`Menu`**
- Title row: `Holcom Cafeteria Menu` + date range
- **`Breakfast`** row starts the breakfast section
- **Categories**: plain text (e.g. `Baguette or Kaak`, `Salads`)
- **Items**: leading dot (e.g. `. Labneh`, `. Turkey & cheese baguette`)
- **Lunch** section begins after the second title block (categories: Cold Sandwich, Salads, Hot Sandwich, Main Dish, Dessert, …)

### Parser implementation

- **Class:** `HolcomMenuExcelParser.cs`
- **Library:** ExcelDataReader (required — reference files contain embedded images incompatible with ClosedXML)
- **Output hierarchy:** Meal → Category → Item (3 levels matching the Excel files)

### Import workflow

1. Upload Excel → parser reads `Menu` sheet
2. All five days auto-populated in the editor
3. Preview modal shows summary (days / categories / items) + per-day tree
4. Staff edits if needed → Save Draft or Publish

## Frontend Structure

```
itg_cafeteria.client/src/
├── api/client.js           API helpers + date utilities
├── context/AuthContext.jsx JWT storage + protected routes
├── components/
│   ├── Layout.jsx
│   ├── MenuTree.jsx        Expand/collapse, inline edit
│   ├── ExcelUpload.jsx     Drag & drop
│   └── WeekSelector.jsx
└── pages/
    ├── PublicMenuPage.jsx  Today's menu + week navigation
    ├── LoginPage.jsx
    ├── PortalPage.jsx    Dashboard
    └── MenuEditorPage.jsx Tree editor + Excel import preview
```

## Workflow

### Staff: Create menu manually
1. Login at `/login`
2. Open Portal → select week → Edit
3. Pick day tab, add categories/subcategories in tree editor
4. Save Draft or Publish

### Staff: Import from Excel
1. Drag Excel onto upload area
2. Review preview modal
3. **Edit Before Save** → adjust in tree editor → Save/Publish  
   **or** Save Draft / Publish directly from preview

### Public: View menu
1. Visit `/` — today's menu highlighted (weekdays only)
2. Navigate weeks with Prev/Next
3. Switch day tabs Mon–Fri

## Configuration

Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=ITG_Cafeteria;..."
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-chars",
    "Issuer": "ITG_Cafeteria",
    "Audience": "ITG_Cafeteria_Users"
  },
  "Seed": {
    "CafeteriaEmail": "cafeteria@itg.com",
    "CafeteriaPassword": "Cafeteria@123"
  }
}
```

## Database Setup

```bash
cd ITG_Cafeteria.Server
dotnet ef database update
```

On first run, `DbSeeder` applies migrations and creates the default cafeteria user.

## Default Credentials

- **Email:** `cafeteria@itg.com`
- **Password:** `Cafeteria@123`

Change these in `Seed` settings before production deployment.

## Running the Application

**Visual Studio:** F5 (starts API + Vite dev server via SPA proxy)

**CLI:**
```bash
# Terminal 1
cd ITG_Cafeteria.Server && dotnet run

# Terminal 2
cd itg_cafeteria.client && npm run dev
```

- API: `https://localhost:7005`
- Swagger: `https://localhost:7005/swagger`
- SPA: `https://localhost:58322`

## Scalability Notes

- Tree stored as flat `MenuNode` rows; rebuild DTO tree in memory (O(n))
- Excel previews cached in-memory by `PreviewId` (replace with Redis for multi-instance)
- Published menus filtered at query level for public endpoints
- Week boundaries normalized to Monday via `WeekHelper`
