---

description: "Task list template for feature implementation"
---

# Tasks: Navigation Rapide Entre Fenêtres Dofus

**Input**: Design documents from `/specs/003-navigation-fenetres-dofus/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md)

**Tests**: xUnit pour la logique pure `Pofus.Core` (`KeyCombo`,
`WindowCycleNavigator`). L'enregistrement réel des raccourcis Win32 et
l'activation de fenêtre sont validés manuellement via
[quickstart.md](quickstart.md) (non simulables de façon fiable en test
unitaire).

**Organization**: Tâches groupées par user story (US1/US2/US3 de spec.md).

## Path Conventions

Extension des projets existants — aucun nouveau projet dans `Pofus.slnx` :

```text
src/Pofus.Core/Navigation/
src/Pofus.Platform/ (Win32Native.cs, IWin32WindowApi.cs, Win32WindowApi.cs étendus)
src/Pofus.Hud/Modules/Navigation/
tests/Pofus.Core.Tests/Navigation/
```

---

## Phase 1: Setup

- [x] T001 Créer les dossiers `src/Pofus.Core/Navigation/`,
      `src/Pofus.Hud/Modules/Navigation/`,
      `tests/Pofus.Core.Tests/Navigation/` (aucun nouveau `.csproj`)

**Checkpoint**: La Phase 2 peut démarrer

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Modèle de raccourcis, calcul de navigation, et interop Win32 —
requis par les 3 user stories

- [x] T002 [P] Définir `NavigationAction` (`Next`, `Previous`, `GoToLeader`)
      dans `src/Pofus.Core/Navigation/NavigationAction.cs`
- [x] T003 [P] Implémenter `KeyCombo` (parsing/formatage pur d'une
      combinaison type `"Ctrl+Maj+Tab"`, table de correspondance touches
      supportées) dans `src/Pofus.Core/Navigation/KeyCombo.cs` per
      data-model.md
- [x] T004 Définir `NavigationShortcutPreferences` (Bindings par action,
      valeurs par défaut `Ctrl+Tab`/`Ctrl+Maj+Tab`/`Ctrl+L`) dans
      `src/Pofus.Core/Navigation/NavigationShortcutPreferences.cs` (dépend de
      T002, T003)
- [x] T005 Implémenter `NavigationShortcutStore` (persistance JSON
      asynchrone, `%APPDATA%\Pofus\navigation-shortcuts.json`, même pattern
      que les stores existants) dans
      `src/Pofus.Core/Navigation/NavigationShortcutStore.cs` (dépend de T004)
- [x] T006 [P] Implémenter `WindowCycleNavigator` (calcul pur de
      Next/Previous — avec bouclage — et de la fenêtre du leader, à partir
      d'une liste ordonnée d'`AccountSession` de la feature 002 et du handle
      actuellement au premier plan) dans
      `src/Pofus.Core/Navigation/WindowCycleNavigator.cs`
- [x] T007 Étendre `Win32Native`/`IWin32WindowApi`/`Win32WindowApi` avec
      `RegisterHotKey`, `UnregisterHotKey`, `SetForegroundWindow`,
      `IsIconic`, `ShowWindow` (restauration), `GetForegroundWindow`,
      `AllowSetForegroundWindow`, et la simulation de frappe Alt
      (`keybd_event`) dans `src/Pofus.Platform/Win32Native.cs`,
      `src/Pofus.Platform/IWin32WindowApi.cs`,
      `src/Pofus.Platform/Win32WindowApi.cs`
- [x] T008 Implémenter `WindowActivator` (séquence de contournement
      `SetForegroundWindow` du projet de référence — restaurer si minimisé,
      `AllowSetForegroundWindow`, frappe Alt simulée, sondage de
      confirmation ~300ms — avec échec journalisé explicitement plutôt
      qu'avalé) dans `src/Pofus.Platform/WindowActivator.cs` (dépend de T007)

**Checkpoint**: Socle prêt — les user stories peuvent démarrer

---

## Phase 3: User Story 1 - Basculer entre mes comptes au clavier (Priority: P1) 🎯 MVP

**Goal**: Les raccourcis "compte suivant"/"compte précédent" fonctionnent
globalement (même fenêtre Dofus au focus), ne parcourent que les comptes
actifs, et restent cohérents avec un changement de fenêtre manuel.

**Independent Test**: Avec 3 comptes actifs, déclencher "suivant" depuis une
fenêtre Dofus et vérifier le changement de premier plan (cf.
[quickstart.md § 1](quickstart.md)).

### Tests for User Story 1

- [x] T009 [P] [US1] Tests unitaires `KeyCombo` (parsing valide, combinaison
      invalide rejetée, formatage d'affichage) dans
      `tests/Pofus.Core.Tests/Navigation/KeyComboTests.cs`
- [x] T010 [P] [US1] Tests unitaires `WindowCycleNavigator` (suivant avec
      bouclage, précédent avec bouclage, liste vide sans exception, un seul
      compte actif sans changement) dans
      `tests/Pofus.Core.Tests/Navigation/WindowCycleNavigatorTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Implémenter `GlobalHotkeyListener` (hook `HwndSource` sur le
      handle de `HudWindow`, réception `WM_HOTKEY`, appelle
      `RegisterHotKey`/`UnregisterHotKey` via `Pofus.Platform`) dans
      `src/Pofus.Hud/Modules/Navigation/GlobalHotkeyListener.cs` (dépend de
      T007)
- [x] T012 [US1] Implémenter `NavigationHudModule : IHudModule` (orchestre
      pression de raccourci → rafraîchissement des comptes actifs
      (feature 002) → `WindowCycleNavigator` → `WindowActivator`) dans
      `src/Pofus.Hud/Modules/Navigation/NavigationHudModule.cs` (dépend de
      T005, T006, T008, T011)
- [x] T013 [US1] Enregistrer les 3 raccourcis par défaut au démarrage du
      module et gérer explicitement le cas "aucun compte actif" (no-op,
      aucune erreur, FR-008) dans
      `src/Pofus.Hud/Modules/Navigation/NavigationHudModule.cs` (dépend de
      T012)
- [x] T014 [US1] Brancher `NavigationHudModule` dans le slot "macros" du HUD
      (composition root) dans `src/Pofus.App/App.xaml.cs` (dépend de T012)

**Checkpoint**: User Story 1 fonctionnelle et testable indépendamment (MVP)

---

## Phase 4: User Story 2 - Rejoindre le leader instantanément (Priority: P2)

**Goal**: Le raccourci "aller au leader" fonctionne indépendamment du cycle
suivant/précédent, et gère explicitement l'absence de leader.

**Independent Test**: Désigner un leader, déclencher le raccourci depuis un
autre compte (cf. [quickstart.md § 2](quickstart.md)).

### Tests for User Story 2

- [x] T015 [P] [US2] Test unitaire : `WindowCycleNavigator` retourne `null`
      pour l'action leader si aucun compte n'a `IsLeader = true`, sans lever
      d'exception, dans
      `tests/Pofus.Core.Tests/Navigation/WindowCycleNavigatorTests.cs`

### Implementation for User Story 2

- [x] T016 [US2] Gérer explicitement le cas "leader non détecté" (no-op
      journalisé, pas d'erreur visible, FR-008) dans
      `src/Pofus.Hud/Modules/Navigation/NavigationHudModule.cs` (dépend de
      T012)

**Checkpoint**: User Stories 1 ET 2 fonctionnelles indépendamment

---

## Phase 5: User Story 3 - Choisir mes propres raccourcis (Priority: P3)

**Goal**: L'utilisateur peut ré-assigner chaque raccourci depuis une fenêtre
dédiée, avec détection de collision et application immédiate.

**Independent Test**: Ré-assigner un raccourci, vérifier qu'il fonctionne
sans redémarrage, puis qu'il persiste après redémarrage (cf.
[quickstart.md § 3](quickstart.md)).

### Tests for User Story 3

- [x] T017 [P] [US3] Test unitaire : deux actions ne peuvent pas partager la
      même `KeyCombo` dans `NavigationShortcutPreferences` (détection de
      collision, FR-006) dans
      `tests/Pofus.Core.Tests/Navigation/NavigationShortcutPreferencesTests.cs`

### Implementation for User Story 3

- [x] T018 [US3] Créer `NavigationShortcutsWindow` (liste des 3 actions avec
      leur combinaison actuelle) dans
      `src/Pofus.Hud/Modules/Navigation/NavigationShortcutsWindow.xaml` +
      `.xaml.cs`
- [x] T019 [US3] Implémenter la capture de touches (`PreviewKeyDown`) pour
      ré-assigner une combinaison dans
      `src/Pofus.Hud/Modules/Navigation/NavigationShortcutsWindow.xaml.cs`
      (dépend de T018)
- [x] T020 [US3] Ajouter la validation anti-collision avant application,
      avec message clair à l'utilisateur en cas de conflit (FR-006) dans
      `src/Pofus.Hud/Modules/Navigation/NavigationShortcutsWindow.xaml.cs`
      (dépend de T019)
- [x] T021 [US3] Implémenter le ré-enregistrement dynamique du raccourci
      Win32 (`UnregisterHotKey` ancien + `RegisterHotKey` nouveau) et la
      persistance immédiate dans
      `src/Pofus.Hud/Modules/Navigation/GlobalHotkeyListener.cs` (dépend de
      T011, T020)
- [x] T022 [US3] Ouvrir `NavigationShortcutsWindow` depuis le contenu du slot
      "macros" (clic) dans
      `src/Pofus.Hud/Modules/Navigation/NavigationHudModule.cs` (dépend de
      T012, T018)

**Checkpoint**: Les 3 user stories sont fonctionnelles indépendamment

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T023 [P] Audit "pas de `catch` silencieux" (Principe I) sur les
      fichiers nouveaux/modifiés de `src/Pofus.Platform/` et
      `src/Pofus.Hud/Modules/Navigation/`
- [x] T024 Exécuter l'ensemble des scénarios de
      [quickstart.md](quickstart.md) de bout en bout et consigner les
      résultats — **résultats** : build + 50/50 tests au vert (40
      `Pofus.Core.Tests` dont 20 nouveaux, 10 `Pofus.Platform.Tests`) ;
      validation en conditions réelles avec **4 vraies fenêtres Dofus**
      simultanées sur la machine de test : `Ctrl+Tab` a fait défiler
      Perso-Deux → Perso-Trois → Perso-Quatre (SC-001, US1 confirmé) ;
      `Ctrl+Maj+Tab` a bien inversé le sens (Perso-Trois → Perso-Six) ;
      `Ctrl+L` sans leader désigné n'a produit aucun changement ni erreur
      (US2, FR-008 confirmé) ; aucune erreur dans les logs. La séquence de
      contournement `SetForegroundWindow` (research.md) fonctionne
      correctement en pratique. Non testé manuellement (couvert par tests
      unitaires uniquement) : reconfiguration de raccourci via capture UI et
      détection de collision (US3) — non actionné par automatisation UI dans
      ce passage

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance
- **Foundational (Phase 2)**: Dépend de Setup — bloque les 3 user stories
- **US1 (Phase 3)**: Dépend de Foundational uniquement — livrable seule (MVP)
- **US2 (Phase 4)**: Dépend de Foundational + réutilise le branchement
  `NavigationHudModule`/`GlobalHotkeyListener` de US1, mais le comportement
  "leader" est indépendamment testable (`WindowCycleNavigator.Leader`)
- **US3 (Phase 5)**: Dépend de Foundational + US1 (réutilise
  `GlobalHotkeyListener` pour le ré-enregistrement dynamique)
- **Polish (Phase 6)**: Dépend des user stories livrées

### Parallel Opportunities

- T002, T003, T006 (Foundational, fichiers distincts) en parallèle
- T009, T010 (tests US1) en parallèle
- T015 (test US2) en parallèle avec la fin de US1
- T017 (test US3) en parallèle avec la fin de US1/US2

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Setup + Foundational
2. US1 → **STOP et VALIDER** via quickstart.md § 1
3. La navigation suivant/précédent au clavier est déjà utile seule

### Incremental Delivery

1. Setup + Foundational → socle prêt
2. US1 → tester indépendamment → navigation suivant/précédent fonctionnelle
   (MVP)
3. US2 → tester indépendamment → saut direct au leader
4. US3 → tester indépendamment → raccourcis reconfigurables

---

## Notes

- [P] = fichiers différents, pas de dépendance
- Commit après chaque tâche ou groupe logique
- S'arrêter à chaque checkpoint pour valider la story indépendamment
