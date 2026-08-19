---

description: "Task list template for feature implementation"
---

# Tasks: Paramètres — Détection de Conflits et Démarrage

**Input**: Design documents from `/specs/004-parametres-conflits-demarrage/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md)

**Tests**: xUnit pour la logique pure `Pofus.Core` (`ConflictingSoftwareDetector`
via un double de `IProcessController`) et pour la persistance. La fermeture de
processus réelle et l'écriture de registre sont validées manuellement via
[quickstart.md](quickstart.md), plus un test d'intégration léger contre une
valeur de registre dédiée aux tests (pas la vraie clé `Run`).

**Organization**: Tâches groupées par user story (US1/US2/US3 de spec.md).

## Path Conventions

Extension des projets existants — aucun nouveau projet dans `Pofus.slnx` :

```text
src/Pofus.Core/Settings/
src/Pofus.Platform/ (ProcessController.cs, StartupRegistration.cs)
src/Pofus.Hud/Modules/Settings/
tests/Pofus.Core.Tests/Settings/
tests/Pofus.Platform.Tests/
```

---

## Phase 1: Setup

- [x] T001 Créer les dossiers `src/Pofus.Core/Settings/`,
      `src/Pofus.Hud/Modules/Settings/`, `tests/Pofus.Core.Tests/Settings/`
      (aucun nouveau `.csproj`)

**Checkpoint**: La Phase 2 peut démarrer

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modèle de préférences, détection de conflit, interop système —
requis par les 3 user stories

- [x] T002 [P] Définir `AppPreferences` (`IgnoreConflictWarning`,
      `LaunchAtStartup`) dans `src/Pofus.Core/Settings/AppPreferences.cs` per
      data-model.md
- [x] T003 Implémenter `AppPreferencesStore` (persistance JSON asynchrone,
      `%APPDATA%\Pofus\app-preferences.json`, même pattern que les stores
      existants) dans `src/Pofus.Core/Settings/AppPreferencesStore.cs`
      (dépend de T002)
- [x] T004 [P] Définir `KnownConflictingSoftware` (liste extensible de noms de
      processus, initialement `["organizer"]`) dans
      `src/Pofus.Core/Settings/KnownConflictingSoftware.cs`
- [x] T005 [P] Définir l'interface `IProcessController`
      (`IsRunning(name)`, `TryKill(name, out error)`) dans
      `src/Pofus.Core/Settings/IProcessController.cs` — dans `Pofus.Core` (pas
      `Pofus.Platform`) pour que `ConflictingSoftwareDetector` reste
      testable sans Windows, même pattern que `IDofusWindowLocator`
      (feature 001/002)
- [x] T006 Implémenter `ConflictingSoftwareDetector`
      (`DetectRunning() : IReadOnlyList<string>`, combine
      `KnownConflictingSoftware` + `IProcessController`) dans
      `src/Pofus.Core/Settings/ConflictingSoftwareDetector.cs` (dépend de
      T004, T005)
- [x] T007 [P] Implémenter `ProcessController` réel (implémente
      `IProcessController` via `System.Diagnostics.Process`, échec de
      `Kill()` capturé et journalisé explicitement plutôt qu'avalé) dans
      `src/Pofus.Platform/ProcessController.cs`
- [x] T008 [P] Implémenter `IStartupRegistration`/`StartupRegistration`
      (`IsEnabled()`, `TryEnable(out error)`, `TryDisable(out error)`, via la
      clé de registre `HKCU\...\Run`, constructeur acceptant un nom de valeur
      configurable pour rester testable sans toucher la vraie clé) dans
      `src/Pofus.Platform/IStartupRegistration.cs`,
      `src/Pofus.Platform/StartupRegistration.cs`

**Checkpoint**: Socle prêt — les user stories peuvent démarrer

---

## Phase 3: User Story 1 - Être alerté d'un logiciel concurrent au démarrage (Priority: P1) 🎯 MVP

**Goal**: Un logiciel concurrent connu déjà en cours d'exécution déclenche un
avertissement explicite au démarrage de Pofus, avec options fermer/continuer/
ignorer définitivement.

**Independent Test**: Lancer un exécutable nommé comme un logiciel concurrent
connu puis démarrer Pofus (cf. [quickstart.md § 1](quickstart.md)).

### Tests for User Story 1

- [x] T009 [P] [US1] Tests unitaires `ConflictingSoftwareDetector` (liste
      vide si rien détecté, détection correcte via un double de
      `IProcessController`, pas d'exception si le nom n'est pas trouvé) dans
      `tests/Pofus.Core.Tests/Settings/ConflictingSoftwareDetectorTests.cs`

### Implementation for User Story 1

- [x] T010 [US1] Créer `ConflictWarningWindow` (message nommant le logiciel
      détecté, boutons Fermer/Continuer, case "Ne plus m'avertir") dans
      `src/Pofus.Hud/Modules/Settings/ConflictWarningWindow.xaml` + `.xaml.cs`
      (dépend de T006)
- [x] T011 [US1] Câbler la détection au démarrage de l'application
      (composition root : exécuter `ConflictingSoftwareDetector`, si
      résultat non vide et `IgnoreConflictWarning == false`, afficher
      `ConflictWarningWindow`) dans `src/Pofus.App/App.xaml.cs` (dépend de
      T003, T006, T007, T010)
- [x] T012 [US1] Gérer explicitement l'échec de fermeture du processus
      concurrent (message d'erreur visible, pas de plantage, FR-011) dans
      `src/Pofus.Hud/Modules/Settings/ConflictWarningWindow.xaml.cs` (dépend
      de T010)

**Checkpoint**: User Story 1 fonctionnelle et testable indépendamment (MVP)

---

## Phase 4: User Story 2 - Lancer Pofus automatiquement avec Windows (Priority: P2)

**Goal**: L'utilisateur active/désactive le lancement automatique de Pofus au
démarrage de Windows depuis une fenêtre de réglages.

**Independent Test**: Activer le lancement automatique, vérifier la clé de
registre (cf. [quickstart.md § 2](quickstart.md)).

### Tests for User Story 2

- [x] T013 [P] [US2] Test d'intégration `StartupRegistration` contre une
      valeur de registre dédiée aux tests (pas la vraie clé `Run` de Pofus) :
      activer, vérifier présence, désactiver, vérifier absence dans
      `tests/Pofus.Platform.Tests/StartupRegistrationTests.cs`

### Implementation for User Story 2

- [x] T014 [US2] Créer `SettingsWindow` avec bascule "Lancer au démarrage de
      Windows" dans `src/Pofus.Hud/Modules/Settings/SettingsWindow.xaml` +
      `.xaml.cs` (dépend de T003, T008)
- [x] T015 [US2] Gérer explicitement l'échec d'activation/désactivation du
      lancement automatique (message d'erreur visible, état affiché non
      modifié, FR-011) dans
      `src/Pofus.Hud/Modules/Settings/SettingsWindow.xaml.cs` (dépend de
      T014)

**Checkpoint**: User Stories 1 ET 2 fonctionnelles indépendamment

---

## Phase 5: User Story 3 - Revenir sur le choix d'ignorer l'avertissement (Priority: P3)

**Goal**: L'utilisateur réinitialise depuis les réglages son choix d'ignorer
définitivement l'avertissement de conflit.

**Independent Test**: Ignorer l'avertissement, le réinitialiser, relancer
Pofus avec le logiciel concurrent actif (cf.
[quickstart.md § 3](quickstart.md)).

### Tests for User Story 3

- [x] T016 [P] [US3] Tests unitaires `AppPreferencesStore` (round-trip
      `IgnoreConflictWarning`/`LaunchAtStartup`, fichier absent au premier
      lancement) dans
      `tests/Pofus.Core.Tests/Settings/AppPreferencesStoreTests.cs`

### Implementation for User Story 3

- [x] T017 [US3] Ajouter le bouton "Réinitialiser l'avertissement de
      conflit" dans `SettingsWindow` (remet `IgnoreConflictWarning` à
      `false` et persiste) dans
      `src/Pofus.Hud/Modules/Settings/SettingsWindow.xaml.cs` (dépend de
      T014)

**Checkpoint**: Les 3 user stories sont fonctionnelles indépendamment

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T018 Implémenter `SettingsHudModule : IHudModule` (contenu compact dans
      le slot "settings", ouvre `SettingsWindow` au clic) dans
      `src/Pofus.Hud/Modules/Settings/SettingsHudModule.cs` (dépend de T014)
- [x] T019 Brancher `SettingsHudModule` dans le slot "settings" du HUD
      (composition root) dans `src/Pofus.App/App.xaml.cs` (dépend de T018)
- [x] T020 [P] Audit "pas de `catch` silencieux" (Principe I) sur les
      fichiers nouveaux/modifiés de `src/Pofus.Platform/` et
      `src/Pofus.Hud/Modules/Settings/`
- [x] T021 Exécuter l'ensemble des scénarios de
      [quickstart.md](quickstart.md) de bout en bout et consigner les
      résultats — **résultats** : build + 64/64 tests au vert (50
      `Pofus.Core.Tests` dont 14 nouveaux, 14 `Pofus.Platform.Tests` dont 4
      nouveaux sur une vraie clé de registre jetable) ; fenêtre Réglages
      ouverte depuis le slot "settings" du HUD, rendu cohérent avec le
      thème ; bascule "Lancer au démarrage de Windows" testée en conditions
      réelles — écriture puis suppression confirmées dans
      `HKCU\...\Run\Pofus` ; aucune erreur dans les logs. Non testé
      manuellement (couvert par tests unitaires uniquement) : l'avertissement
      de conflit lui-même (`organizer.exe` non disponible sur la machine de
      test) et la réinitialisation depuis les réglages

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance
- **Foundational (Phase 2)**: Dépend de Setup — bloque les 3 user stories
- **US1 (Phase 3)**: Dépend de Foundational uniquement — livrable seule (MVP)
- **US2 (Phase 4)**: Dépend de Foundational uniquement — indépendante de US1
- **US3 (Phase 5)**: Dépend de Foundational + réutilise `SettingsWindow` créée
  en US2, mais la réinitialisation elle-même est indépendamment testable
- **Polish (Phase 6)**: Dépend des user stories livrées (le module HUD a
  besoin de `SettingsWindow`, donc au moins US2)

### Parallel Opportunities

- T002, T004, T005 (Foundational, fichiers distincts) en parallèle ; T007,
  T008 en parallèle une fois T005 posé
- T009 (test US1) peut démarrer dès T006 prêt
- T013 (test US2) peut démarrer dès T008 prêt
- US1 et US2 peuvent être menées en parallèle une fois Foundational livré
  (fichiers distincts : `ConflictWarningWindow` vs `SettingsWindow`)

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Setup + Foundational
2. US1 → **STOP et VALIDER** via quickstart.md § 1
3. L'avertissement de conflit est déjà utile seul, sans réglages

### Incremental Delivery

1. Setup + Foundational → socle prêt
2. US1 → tester indépendamment → avertissement de conflit fonctionnel (MVP)
3. US2 → tester indépendamment → lancement automatique
4. US3 → tester indépendamment → réinitialisation de l'avertissement
5. Polish → intégration au HUD (slot "settings")

---

## Notes

- [P] = fichiers différents, pas de dépendance
- Commit après chaque tâche ou groupe logique
- S'arrêter à chaque checkpoint pour valider la story indépendamment
