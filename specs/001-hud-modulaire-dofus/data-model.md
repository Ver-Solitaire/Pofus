# Data Model: HUD Modulaire Style Dofus

## HudLayout

Représente l'état persistant du HUD entre deux sessions (FR-006, SC-005).

| Champ | Type | Description | Règles |
|---|---|---|---|
| `WindowPosition` | `Point (X, Y)` | Position du HUD sur l'écran | Doit rester dans les limites de l'écran visible au chargement (sinon repositionné par défaut) |
| `IsVisible` | `bool` | Le HUD est affiché ou masqué | Valeur par défaut : `true` |
| `Slots` | `List<ModuleSlot>` | Emplacements de modules, dans leur ordre d'agencement fixe | Ordre non modifiable par l'utilisateur en v1 (FR-008) |

## ModuleSlot

Un emplacement du HUD destiné à accueillir un module (Key Entity "Emplacement de
module" de la spec).

| Champ | Type | Description | Règles |
|---|---|---|---|
| `SlotId` | `string` | Identifiant stable de l'emplacement (ex. `"accounts"`, `"macros"`) | Fixe, défini par la disposition (layout) du HUD, pas par l'utilisateur |
| `Position` | `int` | Index d'affichage dans la disposition fixe | Déterminé par la disposition, non éditable par l'utilisateur (FR-008) |
| `State` | `enum { Empty, Occupied }` | Un module est-il rattaché à cet emplacement | `Empty` doit avoir un état visuel distinct (FR-007) |
| `ModuleId` | `string?` | Référence vers le module rattaché, si `Occupied` | `null` si `State == Empty` |

## Module (référence)

Représente un module de l'outil rattaché à un `ModuleSlot`. Le contenu
fonctionnel de chaque module est hors périmètre de cette fonctionnalité (voir
Assumptions de la spec) — seule l'identité minimale nécessaire à son affichage
dans le HUD est modélisée ici. Le contrat complet qu'un module doit implémenter
pour s'intégrer au HUD est décrit dans [contracts/module-contract.md](contracts/module-contract.md).

| Champ | Type | Description | Règles |
|---|---|---|---|
| `ModuleId` | `string` | Identifiant unique du module | Stable, utilisé par `ModuleSlot.ModuleId` |
| `DisplayName` | `string` | Nom lisible du module | Utilisé pour l'accessibilité / infobulles |
| `IconResource` | `Uri` | Icône représentant le module dans le HUD | Doit respecter le style visuel de référence (icônes rondes, Principe/FR-004) |

## TrackedDofusWindows

Représente l'ensemble des fenêtres Dofus actuellement ouvertes, au-dessus
desquelles le HUD unique doit rester affiché (FR-001, FR-005). Ce n'est **pas**
une fenêtre "suivie" au singulier : le HUD reste au-dessus de toutes ces fenêtres
à la fois, quelle que soit celle au premier plan.

| Champ | Type | Description | Règles |
|---|---|---|---|
| `Windows` | `List<WindowHandle>` | Toutes les fenêtres Dofus détectées à l'instant courant | Liste vide si aucune fenêtre Dofus détectée (cf. Edge Cases) |
| `ActiveWindowHandle` | `IntPtr?` (interne) | Handle Win32 de la fenêtre Dofus actuellement au premier plan | Utilisé uniquement pour renseigner `ActiveAccountLabel` ; ne restreint pas l'affichage du HUD aux autres fenêtres |

## ActiveAccountIndicator

Représente le compte Dofus actuellement ciblé par une action du HUD qui
s'applique à un compte précis plutôt qu'à tous (FR-009). Distinct de
`TrackedDofusWindows` : le HUD reste au-dessus de toutes les fenêtres qu'il pilote
même quand un seul compte est "actif" pour une action ciblée.

| Champ | Type | Description | Règles |
|---|---|---|---|
| `ActiveAccountLabel` | `string?` | Étiquette affichée dans le HUD identifiant le compte actif | `null` tant qu'aucune fenêtre Dofus n'est détectée |

## Relations

```text
HudLayout 1 ── * ModuleSlot 0..1 ── 1 Module
HudLayout 1 ── 1 TrackedDofusWindows
HudLayout 1 ── 1 ActiveAccountIndicator
```

- Un `HudLayout` contient plusieurs `ModuleSlot` fixes.
- Un `ModuleSlot` référence au plus un `Module` (via `ModuleId`).
- Un `HudLayout` a un unique `TrackedDofusWindows` (toutes les fenêtres
  au-dessus desquelles il reste affiché) et un unique `ActiveAccountIndicator`
  (HUD partagé, FR-009).

## State Transitions

- `ModuleSlot.State`: `Empty → Occupied` quand un module est rattaché (hors
  périmètre de cette fonctionnalité — modélisé pour que les futures
  fonctionnalités "module" puissent s'y brancher) ; `Occupied → Empty` si le
  module est retiré/désinstallé.
- `ActiveAccountIndicator`: `Aucun → Détecté` quand une fenêtre Dofus apparaît ;
  `Détecté → Aucun` si la fenêtre suivie se ferme ; `Détecté → Détecté(autre)` si
  l'utilisateur change de compte actif.
