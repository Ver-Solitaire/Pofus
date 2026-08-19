# Implementation Plan: Paramètres — Détection de Conflits et Démarrage

**Branch**: `004-parametres-conflits-demarrage` | **Date**: 2026-08-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-parametres-conflits-demarrage/spec.md`

## Summary

Détection au démarrage de Pofus d'un logiciel concurrent connu (liste
extensible, initialement `organizer`), avec avertissement dismissible/
définitivement ignorable, et une fenêtre de réglages généraux (slot "settings"
du HUD) permettant d'activer le lancement automatique au démarrage de Windows
et de réinitialiser l'avertissement de conflit. Réutilise le contrat
`IHudModule`/`ModuleHost` (feature 001) pour l'intégration au HUD.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (LTS) — inchangé

**Primary Dependencies**: `System.Diagnostics.Process` (détection/fermeture de
processus concurrent), `Microsoft.Win32.Registry` (clé `Run` pour le
lancement au démarrage) — toutes deux dans la BCL, aucun nouveau paquet

**Storage**: Fichier JSON local `%APPDATA%\Pofus\app-preferences.json`

**Testing**: xUnit pour `ConflictingSoftwareDetector` et
`AppPreferencesStore` via des doubles de `IProcessController`/
`IStartupRegistration` dans `Pofus.Core.Tests`/`Pofus.Platform.Tests`.
Validation manuelle via [quickstart.md](quickstart.md) pour la fermeture de
processus réelle et l'écriture de registre.

**Target Platform**: Windows 10/11 desktop (inchangé)

**Project Type**: Extension de l'application existante — aucun nouveau projet

**Performance Goals**: Détection en moins de 5s après le lancement (SC-001)

**Constraints**: Aucune opération bloquante sur le thread UI ; échec de
fermeture de processus ou d'écriture de registre signalé explicitement,
jamais silencieux (FR-011)

**Scale/Scope**: Une liste de logiciels concurrents connus (1 entrée pour
l'instant, extensible) ; 2 préférences persistées

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe constitution | Statut | Justification |
|---|---|---|
| I. Robustesse avant tout | PASS | Échec de fermeture de processus ou d'écriture registre explicitement signalé à l'utilisateur (FR-011), jamais avalé — contrairement au `except: pass` du projet de référence |
| II. Fluidité et performance mesurées | PASS | Détection ponctuelle au démarrage uniquement (pas de polling continu) ; aucune opération bloquante sur le thread UI |
| III. Séparation stricte des responsabilités | PASS | Détection/fermeture de processus et écriture de registre dans `Pofus.Platform` derrière des interfaces testables ; préférences et liste de logiciels connus dans `Pofus.Core` (pures, testables sans Windows) |
| IV. Intégration Windows fiable | PASS | `IProcessController`/`IStartupRegistration` encapsulent les appels système avec détection d'échec explicite |
| V. Usage personnel et respect des données locales | PASS | Persistance strictement locale, aucun réseau |

Aucune violation → Complexity Tracking non applicable.

*Re-vérifié après Phase 1 (design) : toujours PASS — aucune complexité
supplémentaire introduite par le modèle de données ou les contrats.*

## Project Structure

### Documentation (this feature)

```text
specs/004-parametres-conflits-demarrage/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── tasks.md
```

### Source Code (repository root)

```text
src/Pofus.Core/
└── Settings/
    ├── AppPreferences.cs
    ├── AppPreferencesStore.cs      # persistance JSON (pattern existant)
    └── KnownConflictingSoftware.cs # liste extensible de noms de processus

src/Pofus.Platform/
├── IProcessController.cs           # IsRunning/TryKill, testable
├── ProcessController.cs
├── IStartupRegistration.cs         # IsEnabled/TryEnable/TryDisable, testable
└── StartupRegistration.cs

src/Pofus.Hud/
└── Modules/Settings/
    ├── SettingsHudModule.cs        # implémente IHudModule (slot "settings")
    ├── ConflictWarningWindow.xaml(.cs)
    └── SettingsWindow.xaml(.cs)

tests/Pofus.Core.Tests/Settings/
└── AppPreferencesStoreTests.cs

tests/Pofus.Platform.Tests/
├── ProcessControllerTests.cs
└── StartupRegistrationTests.cs
```

**Structure Decision**: Extension des projets existants — aucun nouveau
projet. Interop système (processus, registre) dans `Pofus.Platform` derrière
des interfaces ; préférences et données pures dans `Pofus.Core` ; UI (fenêtre
d'avertissement, fenêtre de réglages, module de slot) dans `Pofus.Hud`. Le
module s'intègre au HUD exclusivement via `IHudModule`/`ModuleHost`
(feature 001), sans modification du contrat existant.

## Complexity Tracking

Aucune violation du Constitution Check — section non applicable.
