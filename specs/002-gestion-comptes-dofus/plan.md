# Implementation Plan: Gestion des Comptes Dofus

**Branch**: `002-gestion-comptes-dofus` | **Date**: 2026-08-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-gestion-comptes-dofus/spec.md`

## Summary

Un module de gestion de comptes, hébergé dans le slot "accounts" du HUD
(feature 001) via le contrat `IHudModule` — première implémentation concrète
de ce contrat. Détecte automatiquement les personnages connectés à Dofus
(réutilise `IDofusWindowLocator`), en extrait pseudo/classe, permet de les
activer/désactiver, de les organiser en équipes et de désigner un leader, avec
persistance locale indexée par pseudo (stable à travers les reconnexions). Le
slot affiche un indicateur compact ; le détail s'ouvre dans une fenêtre dédiée
(`AccountManagerWindow`), reprenant le comportement du module équivalent du
projet de référence.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (LTS) — inchangé par rapport à la
feature 001

**Primary Dependencies**: WPF (`Pofus.Hud`), `System.Text.Json`
(persistance), aucune nouvelle dépendance Win32 au-delà de
`IDofusWindowLocator` déjà existant (`Pofus.Platform`)

**Storage**: Fichier JSON local `%APPDATA%\Pofus\account-preferences.json`
(N/A base de données)

**Testing**: xUnit pour `AccountTitleParser`, la fusion détection+préférences,
et la persistance (`Pofus.Core.Tests`) ; validation manuelle via
[quickstart.md](quickstart.md) pour l'UI (`AccountManagerWindow`,
indicateur du slot)

**Target Platform**: Windows 10/11 desktop (inchangé)

**Project Type**: Extension de l'application desktop existante — pas de
nouveau projet dans la solution

**Performance Goals**: Détection d'un nouveau compte en moins de 5s (SC-001,
sondage 2s) ; activation/désactivation perçue comme instantanée (SC-002)

**Constraints**: Aucune opération bloquante sur le thread d'interface
(Principe II) ; persistance strictement locale (Principe V) ; état par pseudo
jamais par handle de fenêtre (les handles changent à chaque reconnexion)

**Scale/Scope**: Jusqu'à une dizaine de comptes simultanés ; jusqu'à 50 pseudos
mémorisés dans l'historique d'ordre (FR-010)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe constitution | Statut | Justification |
|---|---|---|
| I. Robustesse avant tout | PASS | Titre de fenêtre non conforme → "Classe inconnue" explicite (FR-003) ; aucun compte détecté → état géré (FR-011) ; réordonnancement par boutons plutôt que drag-and-drop pour limiter la surface de bugs (research.md) |
| II. Fluidité et performance mesurées | PASS | Sondage 2s (pas de polling agressif), persistance JSON asynchrone, aucun blocage du thread UI |
| III. Séparation stricte des responsabilités | PASS | Parsing + fusion + persistance dans `Pofus.Core` (testable sans WPF) ; UI (`AccountManagerWindow`, module de slot) dans `Pofus.Hud` ; aucune nouvelle dépendance Win32 directe |
| IV. Intégration Windows fiable | PASS | Réutilise `IDofusWindowLocator` (feature 001) déjà robuste aux échecs d'énumération ; aucun nouvel appel Win32 direct |
| V. Usage personnel et respect des données locales | PASS | Persistance strictement locale, aucun réseau |

Aucune violation → Complexity Tracking non applicable.

*Re-vérifié après Phase 1 (design) : toujours PASS — le contrat `IHudModule`
existant absorbe cette fonctionnalité sans modification, confirmant que la
séparation posée en feature 001 tient face au premier module réel.*

## Project Structure

### Documentation (this feature)

```text
specs/002-gestion-comptes-dofus/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── tasks.md
```

### Source Code (repository root)

```text
src/Pofus.Core/
└── Accounts/
    ├── AccountSession.cs
    ├── AccountPreferences.cs
    ├── AccountPreferencesStore.cs      # persistance JSON (pattern HudLayoutStore)
    ├── AccountTitleParser.cs           # extraction pseudo/classe, filtre fenêtres de lancement
    └── AccountDetectionService.cs      # fusion détection brute + préférences → liste AccountSession

src/Pofus.Hud/
└── Modules/Accounts/
    ├── AccountsHudModule.cs            # implémente IHudModule (feature 001)
    ├── AccountManagerWindow.xaml(.cs)  # fenêtre dédiée : liste, toggles, équipes, leader, ordre
    └── AccountRowView.xaml(.cs)        # une ligne de la liste (toggle, classe, équipe, monter/descendre)

tests/Pofus.Core.Tests/Accounts/
├── AccountTitleParserTests.cs
├── AccountDetectionServiceTests.cs
└── AccountPreferencesStoreTests.cs
```

**Structure Decision**: Extension des projets existants (`Pofus.Core`,
`Pofus.Hud`) sous des sous-dossiers `Accounts/` dédiés — aucun nouveau projet
dans `Pofus.slnx`. Cohérent avec Principe III : logique de détection/fusion/
persistance dans `Pofus.Core` (testable sans WPF), UI dans `Pofus.Hud`. Le
module s'intègre au HUD exclusivement via `IHudModule`/`ModuleHost`
(feature 001), sans modification du contrat existant.

## Complexity Tracking

Aucune violation du Constitution Check — section non applicable.
