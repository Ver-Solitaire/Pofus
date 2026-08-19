# Data Model: Masquage des fenêtres de Pofus

> Révisé le 2026-08-19 en même temps que [spec.md](spec.md) : le masquage
> porte sur les fenêtres, plus sur les boutons de module. `ModuleSlot` n'est
> donc pas modifié par cette feature.

## PanelSettings / PanelPreferences (nouveau — `Pofus.Core.Panels`)

Un seul fichier réunit les deux informations persistées par fenêtre (état et
raccourci) : elles sont toujours lues et écrites ensemble, et les séparer en
deux fichiers n'apporterait qu'un risque d'incohérence.

```csharp
public sealed class PanelSettings
{
    public bool IsHidden { get; set; }
    public KeyCombo? ShowShortcut { get; set; }   // null = aucun raccourci
}

public sealed class PanelPreferences
{
    public Dictionary<string, PanelSettings> Panels { get; set; } = [];

    public PanelSettings For(string panelId);              // crée à la volée
    public string? FindShortcutConflict(KeyCombo combo, string excludingPanelId);
}
```

- Clé : `PanelId`, identifiant stable et libre (`"switcher"`, `"hud"`, et les
  fenêtres futures) — ensemble ouvert, d'où un `string` plutôt qu'un enum
  (FR-012).
- Aucun raccourci par défaut (spec.md Assumptions).
- `FindShortcutConflict` ne compare qu'entre fenêtres ; le conflit croisé
  avec les raccourcis de navigation est vérifié par l'appelant, qui a les
  deux jeux de préférences en mémoire.

Persisté par `IPanelPreferencesStore` (`%APPDATA%\Pofus\panels.json`), même
pattern exact que les stores existants : fichier absent → défauts journalisés
en INFO, JSON corrompu → défauts journalisés en ERROR, jamais d'exception
silencieuse (Principe I).

## HideablePanel (nouveau — `Pofus.Hud`)

Ce qu'une fenêtre déclare pour devenir masquable. C'est le seul point à
remplir pour ajouter une fenêtre future (SC-005) :

```csharp
public sealed record HideablePanel(string PanelId, string DisplayName, Window Window);
```

## PanelVisibilityService (nouveau — `Pofus.Hud`)

Le moteur générique. Il ne connaît aucune fenêtre en particulier :

| Membre | Rôle |
|---|---|
| `Register(HideablePanel)` | déclare une fenêtre masquable |
| `Hide(panelId)` | `Window.Hide()` + persistance (FR-002, FR-007) |
| `Show(panelId)` | `Window.Show()` + persistance ; idempotent (FR-002) |
| `IsHidden(panelId)` | état courant |
| `GetPanels()` | pour le menu de notification et l'écran des raccourcis |
| `ApplyPersistedState()` | au démarrage, restaure masqué/affiché (FR-007) |
| `AttachShortcuts(listener)` | enregistre `"panel:{id}"` et route les appuis vers `Show` (FR-004/FR-005) |

`Window.Hide()`/`Show()` conservent la position et l'état interne de la
fenêtre, ce qui satisfait FR-010 sans code supplémentaire.

## GlobalHotkeyListener — convention d'identifiant (inchangée depuis la révision)

| Préfixe | Émis/consommé par |
|---|---|
| `"nav:Next"`, `"nav:Previous"`, `"nav:GoToLeader"` | `NavigationHudModule` |
| `"panel:{PanelId}"` | `PanelVisibilityService` |

Les identifiants `RegisterHotKey` restent attribués par un compteur interne
du listener, garantissant l'absence de collision entre les deux familles.

## Relations

```text
panels.json
└── Panels: Dictionary<PanelId, PanelSettings>
    ├── IsHidden: bool
    └── ShowShortcut: KeyCombo?

PanelVisibilityService (en mémoire)
├── panels enregistrés : PanelId → HideablePanel(Window)
└── liaisons "panel:*" sur le GlobalHotkeyListener partagé → Show(panelId)

navigation-shortcuts.json (inchangé, feature 003)
└── Bindings: Dictionary<NavigationAction, KeyCombo>
```
