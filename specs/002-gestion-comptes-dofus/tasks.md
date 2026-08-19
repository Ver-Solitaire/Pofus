---

description: "Task list template for feature implementation"
---

# Tasks: Gestion des Comptes Dofus

**Input**: Design documents from `/specs/002-gestion-comptes-dofus/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md)

**Tests**: xUnit pour la logique `Pofus.Core` (parsing, fusion détection/préférences,
persistance), cohérent avec le plan. L'UI (`AccountManagerWindow`, indicateur de
slot) est validée manuellement via [quickstart.md](quickstart.md).

**Organization**: Tâches groupées par user story (US1/US2/US3 de spec.md).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, pas de dépendance)
- **[Story]**: US1, US2, US3

## Path Conventions

Extension des projets existants (voir [plan.md § Project Structure](plan.md)) —
aucun nouveau projet dans `Pofus.slnx` :

```text
src/Pofus.Core/Accounts/
src/Pofus.Hud/Modules/Accounts/
tests/Pofus.Core.Tests/Accounts/
```

---

## Phase 1: Setup

- [x] T001 Créer les dossiers `src/Pofus.Core/Accounts/`,
      `src/Pofus.Hud/Modules/Accounts/`, `tests/Pofus.Core.Tests/Accounts/`
      (aucun nouveau `.csproj` — projets existants de la feature 001)

**Checkpoint**: Aucune dépendance bloquante — la Phase 2 peut démarrer

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Détection/fusion/persistance des comptes — requis par les 3 user stories

- [x] T002 [P] Définir `AccountSession` (Pseudo, ClassName, WindowHandle,
      IsActive, Team, IsLeader) dans
      `src/Pofus.Core/Accounts/AccountSession.cs` per data-model.md
- [x] T003 [P] Définir `AccountPreferences` (ActiveByPseudo, TeamByPseudo,
      LeaderPseudo, CustomOrder) dans
      `src/Pofus.Core/Accounts/AccountPreferences.cs` per data-model.md
- [x] T004 Implémenter `AccountPreferencesStore` (persistance JSON asynchrone,
      `%APPDATA%\Pofus\account-preferences.json`, même pattern que
      `HudLayoutStore` de la feature 001 — fichier absent/corrompu géré
      explicitement) dans
      `src/Pofus.Core/Accounts/AccountPreferencesStore.cs` (dépend de T003)
- [x] T005 [P] Implémenter `AccountTitleParser` (exclut les titres vides ou
      commençant par "dofus", découpe `"Pseudo - Classe"`, retourne
      "Classe inconnue" si absente) dans
      `src/Pofus.Core/Accounts/AccountTitleParser.cs` per research.md
- [x] T006 Implémenter `AccountDetectionService` (fusionne
      `IDofusWindowLocator.GetOpenDofusWindows()` + `AccountTitleParser` +
      `AccountPreferencesStore` en une liste ordonnée d'`AccountSession` ;
      gère les doublons de pseudo de façon stable ; purge `CustomOrder`
      au-delà de 50 entrées non détectées, FR-010) dans
      `src/Pofus.Core/Accounts/AccountDetectionService.cs` (dépend de
      T002, T004, T005)

**Checkpoint**: Socle prêt — les user stories peuvent démarrer

---

## Phase 3: User Story 1 - Voir tous mes comptes Dofus d'un coup d'œil (Priority: P1) 🎯 MVP

**Goal**: Le gestionnaire de comptes liste chaque personnage connecté avec
pseudo + classe, exclut les fenêtres de lancement, se rafraîchit
automatiquement.

**Independent Test**: Connecter plusieurs comptes, ouvrir le gestionnaire
depuis le slot "accounts", vérifier la liste (cf.
[quickstart.md § 1](quickstart.md)).

### Tests for User Story 1

- [x] T007 [P] [US1] Tests unitaires `AccountTitleParser` (parsing valide,
      titre de lancement exclu, classe absente → "Classe inconnue") dans
      `tests/Pofus.Core.Tests/Accounts/AccountTitleParserTests.cs`
- [x] T008 [P] [US1] Tests unitaires `AccountDetectionService` (liste vide si
      aucun compte, fusion avec préférences existantes, doublon de pseudo géré
      sans crash) dans
      `tests/Pofus.Core.Tests/Accounts/AccountDetectionServiceTests.cs`

### Implementation for User Story 1

- [x] T009 [US1] Créer `AccountManagerWindow` (liste des comptes, sondage
      2s via `DispatcherTimer` pour rafraîchir, cf. research.md) dans
      `src/Pofus.Hud/Modules/Accounts/AccountManagerWindow.xaml` + `.xaml.cs`
      (dépend de T006)
- [x] T010 [US1] Créer `AccountRowView` (pseudo, classe, état "Classe
      inconnue") dans
      `src/Pofus.Hud/Modules/Accounts/AccountRowView.xaml` + `.xaml.cs`
- [x] T011 [US1] Implémenter `AccountsHudModule : IHudModule` (bouton "Comptes"
      dans le slot, ouvre `AccountManagerWindow` au clic) dans
      `src/Pofus.Hud/Modules/Accounts/AccountsHudModule.cs` (dépend de T009) —
      **simplifié** : bouton texte statique plutôt qu'un compteur de comptes
      actifs en direct ; à améliorer une fois les vraies icônes disponibles
      (feature 001 T027)
- [x] T012 [US1] Brancher `AccountsHudModule` dans le slot "accounts" du HUD
      via `ModuleHost` (feature 001) dans `src/Pofus.Hud/HudWindow.xaml.cs`
      (dépend de T011)
- [x] T013 [US1] Gérer explicitement l'état "aucun compte détecté" dans
      `AccountManagerWindow` (pas de liste vide silencieuse) dans
      `src/Pofus.Hud/Modules/Accounts/AccountManagerWindow.xaml.cs` (dépend
      de T009)

**Checkpoint**: User Story 1 fonctionnelle et testable indépendamment (MVP)

---

## Phase 4: User Story 2 - Choisir quels comptes participent aux actions du HUD (Priority: P2)

**Goal**: Activer/désactiver un compte, état persistant par pseudo.

**Independent Test**: Désactiver un compte, redémarrer Pofus, vérifier la
persistance (cf. [quickstart.md § 2](quickstart.md)).

### Tests for User Story 2

- [x] T014 [P] [US2] Tests unitaires `AccountPreferencesStore` (round-trip
      actif/inactif, fichier absent au premier lancement, état conservé par
      pseudo indépendamment du handle de fenêtre) dans
      `tests/Pofus.Core.Tests/Accounts/AccountPreferencesStoreTests.cs`

### Implementation for User Story 2

- [x] T015 [US2] Ajouter le contrôle actif/inactif (case à cocher) dans
      `AccountRowView`, persisté via `AccountPreferencesStore` dans
      `src/Pofus.Hud/Modules/Accounts/AccountRowView.xaml.cs` (dépend de
      T004, T010)

**Checkpoint**: User Stories 1 ET 2 fonctionnelles indépendamment

---

## Phase 5: User Story 3 - Organiser mes comptes en équipes avec un leader (Priority: P3)

**Goal**: Équipes nommées, leader désignable, ordre réordonnable — tout
persistant par pseudo, y compris à travers une déconnexion temporaire.

**Independent Test**: Créer une équipe, désigner un leader, réordonner,
redémarrer Pofus (cf. [quickstart.md § 3](quickstart.md)).

### Tests for User Story 3

- [x] T016 [P] [US3] Test unitaire : le leader désigné reste `LeaderPseudo`
      même après disparition de son `AccountSession` de la détection (pas de
      transfert automatique) dans
      `tests/Pofus.Core.Tests/Accounts/AccountDetectionServiceTests.cs`

### Implementation for User Story 3

- [x] T017 [P] [US3] Ajouter le champ "équipe" (texte libre ou liste) dans
      `AccountRowView`, persisté via `TeamByPseudo` dans
      `src/Pofus.Hud/Modules/Accounts/AccountRowView.xaml.cs` (dépend de
      T004, T010)
- [x] T018 [US3] Ajouter la désignation de leader (bouton/étoile, un seul
      actif à la fois) dans `AccountRowView` + `AccountManagerWindow`,
      persisté via `LeaderPseudo` dans
      `src/Pofus.Hud/Modules/Accounts/AccountManagerWindow.xaml.cs` (dépend
      de T009, T010)
- [x] T019 [US3] Ajouter les boutons Monter/Descendre par ligne, persistés
      via `CustomOrder` dans
      `src/Pofus.Hud/Modules/Accounts/AccountManagerWindow.xaml.cs` (dépend
      de T009)

**Checkpoint**: Les 3 user stories sont fonctionnelles indépendamment

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T020 [P] Audit "pas de `catch` silencieux" (Principe I) sur
      `src/Pofus.Core/Accounts/` et `src/Pofus.Hud/Modules/Accounts/`
- [x] T021 Exécuter l'ensemble des scénarios de
      [quickstart.md](quickstart.md) de bout en bout et consigner les résultats
      — **résultats** : build + 20/20 tests `Pofus.Core.Tests` (dont 17
      nouveaux pour Accounts) au vert ; lancement réel avec la vraie fenêtre
      Dofus toujours ouverte sur la machine de test — le compte
      "Mon-Perso / Ouginak" détecté et affiché correctement dans
      `AccountManagerWindow` (pseudo, classe, équipe par défaut, case active
      cochée) en ouvrant le gestionnaire depuis le slot "accounts" ;
      `account-preferences.json` correctement créé et peuplé
      (`CustomOrder: ["Mon-Perso"]`) après un cycle de rafraîchissement ;
      aucune erreur dans les logs. Non vérifié manuellement (couvert
      uniquement par tests unitaires, automatisation de clic UI trop fragile
      pour ce passage) : bascule actif/inactif via clic réel, désignation de
      leader, boutons Monter/Descendre, et le cas multi-comptes (un seul
      compte Dofus disponible sur la machine de test)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance
- **Foundational (Phase 2)**: Dépend de Setup — bloque les 3 user stories
- **US1 (Phase 3)**: Dépend de Foundational uniquement — livrable seule (MVP)
- **US2 (Phase 4)**: Dépend de Foundational ; réutilise `AccountRowView` créé
  en US1 mais sa valeur (persistance actif/inactif) est indépendamment
  testable
- **US3 (Phase 5)**: Dépend de Foundational ; réutilise `AccountRowView`/
  `AccountManagerWindow` (US1) mais chaque capacité (équipe, leader, ordre)
  est indépendamment testable
- **Polish (Phase 6)**: Dépend des user stories livrées

### Parallel Opportunities

- T002, T003, T005 (Foundational, fichiers distincts) en parallèle
- T007, T008 (tests US1) en parallèle
- T014 (test US2) en parallèle avec la fin de US1
- T016, T017 (US3) en parallèle
- US2 et US3 peuvent être menées en parallèle une fois US1 livrée (toutes deux
  étendent `AccountRowView`/`AccountManagerWindow`, à coordonner sur ces
  fichiers partagés)

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Setup + Foundational
2. US1 → **STOP et VALIDER** via quickstart.md § 1
3. Le gestionnaire de comptes est déjà utile en lecture seule

### Incremental Delivery

1. Setup + Foundational → socle prêt
2. US1 → tester indépendamment → liste de comptes fonctionnelle (MVP)
3. US2 → tester indépendamment → activation/désactivation persistante
4. US3 → tester indépendamment → équipes, leader, ordre personnalisé

---

## Notes

- [P] = fichiers différents, pas de dépendance
- Commit après chaque tâche ou groupe logique
- S'arrêter à chaque checkpoint pour valider la story indépendamment
