# Data Model: Navigation Rapide Entre Fenêtres Dofus

## NavigationAction (enum)

`Next` | `Previous` | `GoToLeader`

## KeyCombo

Représentation structurée d'une combinaison de touches, indépendante de
Win32 (pure logique de parsing/formatage dans `Pofus.Core`).

| Champ | Type | Description | Règles |
|---|---|---|---|
| `Modifiers` | `KeyModifiers` (flags: Alt, Control, Shift, Win) | Modificateurs requis | Au moins un modificateur recommandé (cf. Assumptions spec.md) mais non forcé techniquement |
| `Key` | `string` | Touche principale (ex. `"Tab"`, `"L"`, `"F9"`) | Doit correspondre à une touche supportée par la table de correspondance |

Formatage d'affichage : `"Ctrl+Maj+Tab"` (ordre Ctrl, Alt, Maj, Win, puis
touche).

## NavigationShortcutPreferences (persisté)

Fichier `%APPDATA%\Pofus\navigation-shortcuts.json`.

| Champ | Type | Description | Règles |
|---|---|---|---|
| `Bindings` | `Dictionary<NavigationAction, KeyCombo>` | Combinaison assignée à chaque action | Les 3 actions doivent avoir une entrée ; valeurs par défaut si fichier absent (FR-007) ; deux actions ne peuvent pas partager la même combinaison (FR-006) |

**Valeurs par défaut** :

| Action | Combinaison par défaut |
|---|---|
| `Next` | `Ctrl+Tab` |
| `Previous` | `Ctrl+Maj+Tab` |
| `GoToLeader` | `Ctrl+L` |

## Relations avec les features précédentes

```text
AccountDetectionService (feature 002) ──> IReadOnlyList<AccountSession>
                                              │
                                              ▼
                              WindowCycleNavigator (nouveau, Pofus.Core)
                                   Next(sessions, currentHwnd)
                                   Previous(sessions, currentHwnd)
                                   Leader(sessions)
                                              │
                                              ▼
                                   nint? targetWindowHandle
                                              │
                                              ▼
                              WindowActivator.TryActivate (Pofus.Platform)
```

- `WindowCycleNavigator` ne connaît que les `AccountSession` déjà filtrées
  (actives) fournies par `AccountDetectionService` — il ne refait aucun
  filtrage lui-même (FR-004 déjà garanti en amont par feature 002).
- Aucune nouvelle entité persistée liée aux comptes ; ce module ajoute
  uniquement `NavigationShortcutPreferences`.

## State Transitions

- **Reconfiguration** : l'utilisateur capture une nouvelle combinaison →
  validation (pas de collision avec les 2 autres actions, FR-006) → si valide,
  désenregistrement de l'ancienne combinaison Win32 + enregistrement de la
  nouvelle + persistance immédiate (FR-005/SC-002) ; si collision, aucun
  changement n'est appliqué et l'utilisateur en est informé.
