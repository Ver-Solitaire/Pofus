# Implementation Plan: HUD Modulaire Style Dofus

**Branch**: `001-hud-modulaire-dofus` | **Date**: 2026-08-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-hud-modulaire-dofus/spec.md`

## Summary

Un HUD desktop unique, en superposition permanente au-dessus de l'ensemble des
fenêtres Dofus ouvertes simultanément (jusqu'à une dizaine de comptes) — pas un
HUD par fenêtre — présentant des emplacements fixes pour héberger les futurs
modules de Pofus, avec une esthétique visuelle reprenant les codes de
l'interface native de Dofus (panneaux sombres à coins arrondis, bordures
dorées/bronze, icônes rondes). Le HUD garde une position fixe à l'écran,
indépendante de la fenêtre Dofus actuellement au premier plan, avec un
indicateur clair du compte actif quand une action cible un compte en
particulier. Implémenté en C#/.NET 10 + WPF pour un rendu fluide et une
intégration Win32 fiable (always-on-top au-dessus de toutes les fenêtres,
transparence sélective).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (LTS)

**Primary Dependencies**: WPF (`Microsoft.NET.Sdk` + `UseWPF`), P/Invoke vers
`user32.dll` (styles de fenêtre calque, `SetWinEventHook`, énumération des
fenêtres), `System.Text.Json` pour la persistance

**Storage**: Fichier JSON local (`%APPDATA%\Pofus\hud-layout.json`) — pas de base
de données

**Testing**: xUnit pour la logique métier isolée (persistance, détection de
fenêtre active, calcul de disposition) ; validation manuelle via
[quickstart.md](quickstart.md) pour le rendu visuel et le comportement d'overlay
réel

**Target Platform**: Windows 10/11 desktop

**Project Type**: desktop-app (application WPF unique)

**Performance Goals**: Rendu à 60 fps ; affichage/masquage du HUD perçu comme
instantané (SC-004) ; aucune désynchronisation sur 30+ minutes d'utilisation
continue (SC-001)

**Constraints**: Always-on-top au-dessus du jeu sans voler le focus clavier ;
aucune opération bloquante sur le thread d'interface (Principe II) ; aucune
transmission réseau (Principe V)

**Scale/Scope**: Poste de travail unique, un seul utilisateur ; une poignée
d'emplacements de modules (5 à 8, d'après les captures de référence) ; jusqu'à
une dizaine de fenêtres Dofus simultanées, toutes pilotées par un HUD unique à
position fixe (FR-001, FR-009) — pas d'instance de HUD par fenêtre

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe constitution | Statut | Justification |
|---|---|---|
| I. Robustesse avant tout | PASS | Contrat module ([contracts/module-contract.md](contracts/module-contract.md)) impose capture explicite + log de toute exception module ; détection de fenêtre Dofus propage un état explicite au lieu d'avaler les erreurs (research.md) |
| II. Fluidité et performance mesurées | PASS | WPF + rendu GPU ; hook d'évènement (`EVENT_SYSTEM_FOREGROUND`) plutôt que polling ; persistance JSON asynchrone ; objectifs 60 fps / <100ms perçu chiffrés dans Technical Context |
| III. Séparation stricte des responsabilités | PASS | Structure en projets distincts : HUD/UI, contrat module, persistance, interop Win32 (voir Project Structure) — logique testable indépendamment du rendu WPF |
| IV. Intégration Windows fiable | PASS | Interop Win32 encapsulée dans un projet dédié (`Pofus.Platform`), détection d'échec explicite (fenêtre introuvable = état géré, pas une exception silencieuse) |
| V. Usage personnel et respect des données locales | PASS | Persistance strictement locale (fichier JSON `%APPDATA%`), aucun appel réseau dans cette fonctionnalité |

Aucune violation → section Complexity Tracking non applicable.

*Re-vérifié après Phase 1 (design) : toujours PASS sur les 5 principes — le
contrat module et le modèle de données (Phase 1) renforcent Principe I (gestion
d'erreur par module) et Principe III (le contenu des modules est explicitement
hors périmètre du HUD) sans introduire de complexité supplémentaire.*

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
Pofus.slnx

src/
├── Pofus.App/              # Point d'entrée WPF (App.xaml, composition root)
├── Pofus.Hud/               # Fenêtre HUD, contrôle ModuleSlot, gestion disposition
├── Pofus.Core/               # Modèle de données (HudLayout, ModuleSlot, Module),
│                              contrat IHudModule
└── Pofus.Platform/          # Interop Win32 : détection fenêtre Dofus, styles de
                               fenêtre calque, hook EVENT_SYSTEM_FOREGROUND

tests/
├── Pofus.Core.Tests/         # Logique de disposition, modèle, contrat
├── Pofus.Platform.Tests/     # Détection de fenêtre (abstraite derrière une
│                              interface pour être mockable), persistance JSON
```

**Structure Decision**: Solution .NET multi-projets (convention standard .NET
plutôt que le layout générique `src/models|services|cli|lib`) — un seul
exécutable desktop (`Pofus.App`), avec la logique métier isolée dans
`Pofus.Core` et l'interop Win32 isolée dans `Pofus.Platform` pour respecter le
Principe III (séparation des responsabilités) et rendre la logique testable sans
dépendre du rendu WPF réel. `Pofus.Hud` contient la fenêtre et les contrôles
WPF ; il consomme `Pofus.Core` (modèle + contrat module) et `Pofus.Platform`
(suivi de fenêtre) sans logique métier propre.

## Complexity Tracking

Aucune violation du Constitution Check — section non applicable.
