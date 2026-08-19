# Implementation Plan: Navigation Rapide Entre Fenêtres Dofus

**Branch**: `003-navigation-fenetres-dofus` | **Date**: 2026-08-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-navigation-fenetres-dofus/spec.md`

## Summary

Navigation clavier globale entre les fenêtres Dofus actives : compte suivant,
précédent, et saut direct au leader (désigné en feature 002), déclenchée par
des raccourcis système (`RegisterHotKey`) fonctionnant même quand une fenêtre
Dofus a le focus. Les raccourcis sont reconfigurables et persistants. Réutilise
`AccountDetectionService` (feature 002) pour la liste des comptes actifs et
`IHudModule`/`ModuleHost` (feature 001) pour l'intégration au HUD, dans le
slot "macros". L'activation de fenêtre reprend la séquence de contournement
`SetForegroundWindow` du projet de référence, avec gestion d'erreur explicite
plutôt qu'avalée.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (LTS) — inchangé

**Primary Dependencies**: `RegisterHotKey`/`UnregisterHotKey`/
`SetForegroundWindow`/`AllowSetForegroundWindow` (user32.dll, nouveaux dans
`Pofus.Platform`), `HwndSource` (WPF, `Pofus.Hud`, pour recevoir `WM_HOTKEY`
sur la fenêtre HUD existante), `System.Text.Json` (persistance)

**Storage**: Fichier JSON local `%APPDATA%\Pofus\navigation-shortcuts.json`

**Testing**: xUnit pour `KeyCombo` (parsing/formatage) et
`WindowCycleNavigator` (calcul suivant/précédent/leader) dans
`Pofus.Core.Tests` — logique pure, sans Win32. Validation manuelle via
[quickstart.md](quickstart.md) pour l'enregistrement réel des raccourcis et
l'activation de fenêtre (non simulables de façon fiable en test unitaire).

**Target Platform**: Windows 10/11 desktop (inchangé)

**Project Type**: Extension de l'application existante — aucun nouveau projet

**Performance Goals**: Bascule de fenêtre perçue en moins d'une seconde
(SC-001) ; reconfiguration d'un raccourci appliquée immédiatement (SC-002)

**Constraints**: Raccourcis actifs quelle que soit la fenêtre au premier plan
(FR-009) ; aucun blocage du thread UI ; persistance strictement locale

**Scale/Scope**: 3 actions de navigation, jusqu'à une dizaine de comptes actifs
à parcourir

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe constitution | Statut | Justification |
|---|---|---|
| I. Robustesse avant tout | PASS | `WindowActivator` journalise explicitement tout échec d'activation (research.md) — contrairement au `except: pass` du projet de référence ; capture de touches côté UI plutôt que saisie texte libre évite une classe d'erreurs de parsing |
| II. Fluidité et performance mesurées | PASS | `RegisterHotKey` est piloté par évènement (`WM_HOTKEY`), aucun polling ; le sondage court de confirmation d'activation (~300ms max, hérité du projet de référence) est borné et ponctuel |
| III. Séparation stricte des responsabilités | PASS | `WindowCycleNavigator`/`KeyCombo` (logique pure) dans `Pofus.Core`, testables sans WPF ni Win32 ; interop Win32 (`RegisterHotKey`, activation de fenêtre) dans `Pofus.Platform` ; réception `WM_HOTKEY` (nécessite `HwndSource`, WPF) dans `Pofus.Hud` |
| IV. Intégration Windows fiable | PASS | Conflit de raccourci détecté via la valeur de retour native de `RegisterHotKey` (research.md) ; activation de fenêtre avec détection d'échec explicite plutôt que supposée réussie |
| V. Usage personnel et respect des données locales | PASS | Persistance strictement locale, aucun réseau |

Aucune violation → Complexity Tracking non applicable.

*Re-vérifié après Phase 1 (design) : toujours PASS — `WindowCycleNavigator`
consomme uniquement les `AccountSession` déjà filtrées par la feature 002,
sans dupliquer de logique de filtrage (renforce Principe III).*

## Project Structure

### Documentation (this feature)

```text
specs/003-navigation-fenetres-dofus/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── tasks.md
```

### Source Code (repository root)

```text
src/Pofus.Core/
└── Navigation/
    ├── NavigationAction.cs
    ├── KeyCombo.cs                     # parsing/formatage pur, sans Win32
    ├── NavigationShortcutPreferences.cs
    ├── NavigationShortcutStore.cs      # persistance JSON (pattern existant)
    └── WindowCycleNavigator.cs         # calcul suivant/précédent/leader

src/Pofus.Platform/
├── Win32Native.cs                      # + RegisterHotKey, UnregisterHotKey,
│                                          SetForegroundWindow, IsIconic,
│                                          ShowWindow, GetForegroundWindow,
│                                          AllowSetForegroundWindow, keybd_event
├── IWin32WindowApi.cs                  # + méthodes correspondantes
├── Win32WindowApi.cs                   # + implémentation
└── WindowActivator.cs                  # séquence de contournement SetForegroundWindow

src/Pofus.Hud/
└── Modules/Navigation/
    ├── GlobalHotkeyListener.cs         # HwndSource hook sur HudWindow, WM_HOTKEY
    ├── NavigationHudModule.cs          # implémente IHudModule (slot "macros")
    └── NavigationShortcutsWindow.xaml(.cs)  # configuration des 3 raccourcis

tests/Pofus.Core.Tests/Navigation/
├── KeyComboTests.cs
└── WindowCycleNavigatorTests.cs
```

**Structure Decision**: Extension des projets existants, sous-dossiers
`Navigation/` dédiés — aucun nouveau projet. `WindowCycleNavigator` et
`KeyCombo` restent purement logiques (Pofus.Core, testables sans Windows) ;
l'interop Win32 pour les raccourcis globaux et l'activation de fenêtre vit
dans `Pofus.Platform` ; seule la réception effective de `WM_HOTKEY` (qui
nécessite `HwndSource`, une API WPF) vit dans `Pofus.Hud`, attachée à la
`HudWindow` déjà existante — pas de fenêtre invisible supplémentaire.

## Complexity Tracking

Aucune violation du Constitution Check — section non applicable.
