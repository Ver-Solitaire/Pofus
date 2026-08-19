# Implementation Plan: Masquage des fenêtres de Pofus

**Branch**: `005-masquage-modules-hud` | **Date**: 2026-08-19 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-masquage-modules-hud/spec.md`

> **Révision du 2026-08-19** — le périmètre initial (masquer les *boutons de
> module* dans la barre HUD) était une mauvaise lecture du besoin. Il porte
> désormais sur les *fenêtres* elles-mêmes. Ce qui reste valable du plan
> initial : la généralisation du `GlobalHotkeyListener` (section « Technical
> Context » et research.md #2) et le choix d'un fichier de préférences séparé.
> Ce qui est abandonné : l'ajout de `ModuleSlot.IsHidden` (le modèle de layout
> n'est pas touché) et le menu contextuel porté par `ModuleSlotView`.

## Summary

Chaque fenêtre de Pofus (widget des personnages, barre HUD, et toute fenêtre
future) peut être masquée par un clic droit → « Masquer cette fenêtre », et
réaffichée par un raccourci clavier global dédié, configurable par fenêtre
avec détection de conflit — en réutilisant et généralisant le mécanisme de
raccourcis déjà livré pour la navigation (feature 003). Un moteur générique
(`PanelVisibilityService`) porte le masquage, la persistance et les
raccourcis, si bien qu'une nouvelle fenêtre devient masquable par sa seule
déclaration. L'état et les liaisons vivent dans un nouveau fichier
`%APPDATA%\Pofus\panels.json`, séparé de celui des raccourcis de navigation.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (inchangé — aucune nouvelle dépendance)

**Primary Dependencies**: WPF (`System.Windows`), P/Invoke Win32 existant
(`RegisterHotKey`/`UnregisterHotKey` déjà encapsulés dans `Pofus.Platform`)

**Storage**: fichiers JSON locaux sous `%APPDATA%\Pofus` — extension du
fichier existant `hud-layout.json` (nouvel attribut `IsHidden` par slot) +
nouveau fichier `module-shortcuts.json` (même forme que
`navigation-shortcuts.json`)

**Testing**: xUnit (`Pofus.Core.Tests`, `Pofus.Platform.Tests`), même
approche que les features 001-004 : logique pure testée sans Windows,
interop Win32 validée manuellement + tests d'intégration légers là où
possible

**Target Platform**: Windows 10/11 (inchangé)

**Project Type**: desktop-app — extension des projets existants, aucun
nouveau projet dans `Pofus.slnx`

**Performance Goals**: réaffichage d'un module masqué en moins d'une seconde
après l'appui sur son raccourci (SC-002) — trivial, le dispatch WM_HOTKEY
existant est déjà quasi instantané ; le re-rendu de la barre du HUD ne fait
aucune E/S sur le thread UI

**Constraints**: Principe II (aucun blocage du thread UI) ; l'espace d'ID
interne utilisé par `RegisterHotKey` (un `int` par combinaison enregistrée au
niveau du processus) doit rester sans collision entre les actions de
navigation existantes et les nouvelles actions « réafficher le module X »

**Scale/Scope**: 3 modules actuellement masquables (Comptes, Navigation,
Réglages) ; conçu pour rester correct sans modification à mesure que
d'autres slots ("group-actions", "radial-menu") seront occupés par de
futurs modules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Robustesse avant tout** : tout échec d'enregistrement d'un raccourci
  (module ou navigation) est déjà journalisé explicitement par
  `GlobalHotkeyListener` (feature 003) — le comportement est conservé et
  étendu, pas réinventé. PASS.
- **II. Fluidité et performance mesurées** : le dispatch reste piloté par
  `WM_HOTKEY` (événementiel, pas de polling) ; masquer/réafficher un slot ne
  fait que muter une liste en mémoire et réexécuter le rendu déjà existant de
  la barre — aucune E/S synchrone sur le thread UI. PASS.
- **III. Séparation stricte des responsabilités** : l'état de masquage
  (`ModuleSlot.IsHidden`) et les préférences de raccourci
  (`ModuleShortcutPreferences`) vivent dans `Pofus.Core` (testables sans
  Windows) ; `RegisterHotKey`/`UnregisterHotKey` restent encapsulés dans
  `Pofus.Platform` (inchangé) ; seule l'UI (bouton « Masquer », rendu de la
  barre, capture de combinaison) vit dans `Pofus.Hud`. PASS.
- **IV. Intégration Windows fiable** : la généralisation de
  `GlobalHotkeyListener` (clé `string` au lieu de `NavigationAction`) garde
  la détection explicite d'échec de `RegisterHotKey` (conflit avec un autre
  logiciel) — comportement inchangé, juste généralisé. PASS.
- **V. Usage personnel et respect des données locales** : persistance locale
  uniquement, aucun réseau. PASS.

Aucune violation — pas de Complexity Tracking nécessaire.

## Project Structure

### Documentation (this feature)

```text
specs/005-masquage-modules-hud/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

(Pas de `contracts/` — application desktop interne, aucune interface externe
exposée, cohérent avec les features 001-004.)

### Source Code (repository root)

```text
src/Pofus.Core/
├── Models/
│   └── ModuleSlot.cs                    # + IsHidden (extension, pas de nouveau fichier)
└── Modules/                              # nouveau sous-dossier, miroir de Navigation/
    ├── ModuleShortcutPreferences.cs      # Dictionary<string ModuleId, KeyCombo> + FindConflict
    └── ModuleShortcutStore.cs            # IModuleShortcutStore, JSON à module-shortcuts.json

src/Pofus.Hud/
├── HudWindow.xaml.cs                     # hide-button handling, dispatch hotkey "module:*" → unhide + re-render + persist
├── Controls/
│   ├── ModuleSlotView.xaml               # + ContextMenu "Masquer ce module" (State == Occupied uniquement)
│   └── ModuleSlotView.xaml.cs            # + HideRequested event
└── Modules/Navigation/
    ├── GlobalHotkeyListener.cs           # généralisé : clé string au lieu de NavigationAction
    ├── NavigationHudModule.cs            # adapte l'appel à GlobalHotkeyListener (mapping interne NavigationAction <-> "nav:*")
    └── NavigationShortcutsWindow.xaml(.cs) # étendu : une ligne de capture par module masquable, conflit croisé nav/module

tests/Pofus.Core.Tests/
├── Models/ModuleSlotTests.cs (si absent, sinon extension)
└── Modules/ModuleShortcutPreferencesTests.cs, ModuleShortcutStoreTests.cs

tests/Pofus.Hud.Tests/ (si créé) ou validation manuelle via quickstart.md pour
GlobalHotkeyListener (déjà couvert manuellement en feature 003, pas de projet
de test dédié à Pofus.Hud actuellement)
```

**Structure Decision**: Extension des projets existants (`Pofus.Core`,
`Pofus.Hud`) — aucun nouveau projet. L'état de masquage rejoint le modèle de
layout déjà persisté (`HudLayout`/`ModuleSlot`, feature 001) plutôt que de
créer un nouveau store séparé, car c'est déjà exactement la responsabilité de
ce modèle (état persistant d'un slot). Les raccourcis par module reçoivent en
revanche leur propre fichier de préférences, séparé de celui de la
navigation, pour ne pas risquer de perturber le format déjà en production du
fichier `navigation-shortcuts.json` (cohérent avec l'hypothèse du spec : « ne
pas casser les raccourcis de navigation existants »).
