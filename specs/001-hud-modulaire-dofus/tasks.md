---

description: "Task list template for feature implementation"
---

# Tasks: HUD Modulaire Style Dofus

**Input**: Design documents from `/specs/001-hud-modulaire-dofus/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/module-contract.md](contracts/module-contract.md), [quickstart.md](quickstart.md)

**Tests**: Le plan (Technical Context) définit xUnit pour la logique testable
indépendamment du rendu WPF (Principe III). Des tâches de test unitaire sont donc
incluses pour `Pofus.Core` et `Pofus.Platform` ; le rendu visuel et le
comportement d'overlay réel restent validés manuellement via
[quickstart.md](quickstart.md) (non unit-testables de façon fiable, cf. research.md).

**Organization**: Tâches groupées par user story (US1/US2/US3, priorités P1/P2/P3
de spec.md) pour permettre une implémentation et une validation indépendantes de
chacune.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Peut s'exécuter en parallèle (fichiers différents, pas de dépendance)
- **[Story]**: User story concernée (US1, US2, US3)
- Chemins de fichiers exacts inclus dans chaque description

## Path Conventions

Solution .NET multi-projets (voir [plan.md § Project Structure](plan.md)) :

```text
Pofus.slnx
src/Pofus.App/        # Composition root WPF
src/Pofus.Hud/          # Fenêtre HUD, contrôles WPF
src/Pofus.Core/          # Modèle de données, contrat IHudModule, persistance
src/Pofus.Platform/     # Interop Win32
tests/Pofus.Core.Tests/
tests/Pofus.Platform.Tests/
```

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialisation de la solution .NET et de sa structure de projets

- [x] T001 Créer la solution `Pofus.slnx` et les 4 projets applicatifs
      (`src/Pofus.App`, `src/Pofus.Hud`, `src/Pofus.Core`, `src/Pofus.Platform`)
      per [plan.md § Project Structure](plan.md), ciblant .NET 10
- [x] T002 [P] Configurer `src/Pofus.App/Pofus.App.csproj` comme exécutable WPF
      (`<UseWPF>true</UseWPF>`, `OutputType=WinExe`) et référencer
      `Pofus.Hud`
- [x] T003 [P] Configurer les références de projet : `Pofus.Hud` → `Pofus.Core` +
      `Pofus.Platform` ; `Pofus.Platform` ne référence que `Pofus.Core`
      (aucune dépendance inverse, cf. Principe III)
- [x] T004 [P] Créer les projets de test `tests/Pofus.Core.Tests` et
      `tests/Pofus.Platform.Tests` (xUnit), référencés à leurs projets respectifs,
      et les ajouter à `Pofus.slnx`
- [x] T005 [P] Ajouter un `.gitignore` .NET standard (`bin/`, `obj/`, `*.user`) à
      la racine du dépôt

**Checkpoint**: `dotnet build` réussit sur la solution vide avant toute logique métier

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Socle commun requis par les 3 user stories — **rien dans les phases
suivantes ne doit commencer avant que cette phase soit terminée**

- [x] T006 [P] Définir l'interface `IHudModule` — **implémentée dans
      `src/Pofus.Hud/IHudModule.cs`, pas `Pofus.Core`** : la méthode
      `CreateContent()` retourne un `UIElement` WPF, une dépendance que
      `Pofus.Core` ne porte délibérément pas (Principe III — Core reste
      testable sans WPF) ; conforme à
      [contracts/module-contract.md](contracts/module-contract.md) par ailleurs
- [x] T007 [P] Définir les modèles `HudLayout`, `ModuleSlot`, `ModuleDescriptor`
      (`Module` renommé pour éviter la confusion avec `IHudModule`),
      `TrackedDofusWindows`, `ActiveAccountIndicator` dans
      `src/Pofus.Core/Models/` per [data-model.md](data-model.md)
- [x] T008 Implémenter la persistance JSON asynchrone de `HudLayout`
      (`%APPDATA%\Pofus\hud-layout.json`, lecture/écriture non bloquantes) dans
      `src/Pofus.Core/Persistence/HudLayoutStore.cs` (dépend de T007)
- [x] T009 [P] Implémenter l'énumération des fenêtres Dofus ouvertes
      (`EnumWindows` + `GetWindowThreadProcessId`, retour d'une liste explicite,
      jamais d'exception avalée) dans
      `src/Pofus.Platform/DofusWindowLocator.cs` per research.md § Détection des
      fenêtres Dofus — appels Win32 abstraits derrière `IWin32WindowApi` /
      `IProcessNameResolver` pour rester unit-testables (voir T013)
- [x] T010 [P] Implémenter le contrôleur de fenêtre calque/always-on-top
      (`SetWindowPos(HWND_TOPMOST)`, styles `WS_EX_LAYERED`) dans
      `src/Pofus.Platform/TopmostWindowController.cs`
- [x] T011 [P] Implémenter le watcher d'évènement de changement de fenêtre au
      premier plan (`SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`, sans filtrage sur
      un processus précis) dans
      `src/Pofus.Platform/ForegroundWindowWatcher.cs`
- [x] T012 Mettre en place un logger centralisé (fichier + Debug) utilisé par
      `Pofus.Platform` et `Pofus.Core` pour toute exception interceptée
      (Principe I — aucun `catch` silencieux) dans
      `src/Pofus.Core/Logging/FileAppLogger.cs` (+ `IAppLogger`)

**Checkpoint**: Socle prêt — les user stories peuvent démarrer

---

## Phase 3: User Story 1 - Afficher un HUD unique par-dessus toutes les fenêtres Dofus (Priority: P1) 🎯 MVP

**Goal**: Un HUD WPF unique s'affiche en superposition, reste au-dessus de
l'ensemble des fenêtres Dofus ouvertes quelle que soit celle au premier plan, et
peut être affiché/masqué instantanément.

**Independent Test**: Ouvrir plusieurs fenêtres Dofus, afficher le HUD, faire
passer chaque fenêtre au premier plan tour à tour : le HUD reste visible
au-dessus de toutes, sans se dupliquer ni disparaître (cf.
[quickstart.md § 1, 2, 5](quickstart.md)).

### Tests for User Story 1

- [x] T013 [P] [US1] Tests unitaires de `DofusWindowLocator` (liste non vide,
      liste vide si aucune fenêtre Dofus, robustesse à un échec d'énumération)
      dans `tests/Pofus.Platform.Tests/DofusWindowLocatorTests.cs` — 7 tests
- [x] T014 [P] [US1] Tests unitaires de la logique de réassertion du
      `TopmostWindowController` (appel `HWND_TOPMOST` déclenché à chaque
      évènement de premier plan, via une abstraction mockable de l'API Win32)
      dans `tests/Pofus.Platform.Tests/TopmostWindowControllerTests.cs` — 3 tests

### Implementation for User Story 1

- [x] T015 [US1] Créer la fenêtre `HudWindow` WPF (`AllowsTransparency=True`,
      `WindowStyle=None`, `Topmost=True`, `ShowInTaskbar=False`) dans
      `src/Pofus.Hud/HudWindow.xaml` + `HudWindow.xaml.cs`
- [x] T016 [US1] Brancher `ForegroundWindowWatcher` sur `HudWindow` pour
      réaffirmer `HWND_TOPMOST` à chaque changement de fenêtre au premier plan
      dans `src/Pofus.Hud/HudWindow.xaml.cs` (dépend de T011, T015)
- [x] T017 [US1] Implémenter l'affichage/masquage instantané du HUD
      (`HudVisibilityController`, sans fermer l'application) dans
      `src/Pofus.Hud/HudVisibilityController.cs` (dépend de T015) — déclenché
      via une icône systray (`Pofus.App/TrayIconController.cs`, ajoutée en
      Polish) car FR-003 exige un moyen pour l'utilisateur de basculer la
      visibilité, absent de la tâche d'origine
- [x] T018 [US1] Mettre à jour `ActiveAccountIndicator` à partir de
      `DofusWindowLocator` + des évènements de premier plan dans
      `src/Pofus.Hud/ActiveAccountPresenter.cs` (dépend de T009, T011, T007)
- [x] T019 [US1] Gérer explicitement l'état "aucune fenêtre Dofus détectée" dans
      l'UI du HUD (pas de crash, indicateur visuel dédié — cf. Edge Cases spec.md)
      dans `src/Pofus.Hud/HudWindow.xaml.cs` (dépend de T009, T015)
- [x] T020 [US1] Câbler le point d'entrée `Pofus.App` (composition root :
      instancier les services `Pofus.Platform`/`Pofus.Core` et `HudWindow`) dans
      `src/Pofus.App/App.xaml.cs` (dépend de T015–T019)

**Checkpoint**: User Story 1 fonctionnelle et testable indépendamment (MVP) —
validée manuellement : HUD affiché au-dessus d'autres fenêtres, détection d'une
vraie fenêtre Dofus en cours d'exécution, état "aucun compte" correct sans
Dofus lancé (voir T033)

---

## Phase 4: User Story 2 - Organiser les modules dans le HUD (Priority: P2)

**Goal**: Le HUD affiche des emplacements de modules fixes et identifiables,
avec un état visuel "vide" explicite, et conserve sa position/disposition entre
deux lancements.

**Independent Test**: Observer les emplacements du HUD (identifiables sans
explication), déplacer le HUD, redémarrer l'outil, vérifier que la position et
l'état des emplacements sont restaurés (cf.
[quickstart.md § 3](quickstart.md)).

### Tests for User Story 2

- [x] T021 [P] [US2] Tests unitaires de `HudLayoutStore` (sauvegarde/chargement,
      fichier absent au premier lancement, fichier corrompu géré explicitement
      plutôt que de crasher) dans
      `tests/Pofus.Core.Tests/HudLayoutStoreTests.cs` — 3 tests

### Implementation for User Story 2

- [x] T022 [P] [US2] Créer le contrôle WPF `ModuleSlotView` (états visuels
      Occupied/Empty distincts, icône/libellé) dans
      `src/Pofus.Hud/Controls/ModuleSlotView.xaml` + `.xaml.cs`
- [x] T023 [US2] Afficher l'ensemble fixe des `ModuleSlot` (5 slots par défaut :
      accounts, macros, group-actions, radial-menu, settings — voir
      `DefaultHudLayoutFactory`) dans `HudWindow` dans
      `src/Pofus.Hud/HudWindow.xaml` (dépend de T022, T015)
- [x] T024 [US2] Charger le `HudLayout` persisté au démarrage et appliquer la
      position du HUD + l'état des emplacements dans
      `src/Pofus.App/App.xaml.cs` (dépend de T008, T007, T020)
- [x] T025 [US2] Sauvegarder la position du HUD de façon asynchrone (débounce
      500ms, non bloquante) à chaque déplacement/fermeture dans
      `src/Pofus.Hud/HudWindow.xaml.cs` (dépend de T008, T015)
- [x] T026 [US2] Implémenter le déplacement du HUD dans son ensemble
      (glisser-déposer de la fenêtre entière via `DragMove()`, pas des
      emplacements internes) dans
      `src/Pofus.Hud/HudWindow.xaml.cs` (dépend de T015)

**Checkpoint**: User Stories 1 ET 2 fonctionnelles indépendamment — validé :
position restaurée à l'identique sur 2 lancements consécutifs sans dérive
(voir T033)

---

## Phase 5: User Story 3 - Bénéficier d'une esthétique cohérente avec Dofus (Priority: P3)

**Goal**: Le HUD reprend visuellement les codes de l'interface Dofus (panneaux
sombres à coins arrondis, bordures dorées/bronze, icônes rondes) au point d'être
identifié comme faisant partie du jeu.

**Independent Test**: Comparaison côte à côte du HUD avec les captures de
référence Dofus (cf. [quickstart.md § 4](quickstart.md)).

### Implementation for User Story 3

*(Pas de tests unitaires dédiés — rendu purement visuel, validé manuellement via
quickstart.md, cf. research.md § Stratégie de test)*

- [ ] T027 [P] [US3] **NON FAIT** — Importer/préparer les ressources d'icônes
      rondes conformes aux captures de référence dans
      `src/Pofus.Hud/Assets/Icons/`. Différé faute d'assets image réels
      disponibles (les captures de référence n'existent que comme images
      partagées en conversation, pas comme fichiers exploitables) — fabriquer
      de fausses icônes aurait été pire que de laisser les slots afficher un
      libellé texte pour l'instant. À refaire quand de vraies icônes seront
      fournies.
- [x] T028 [P] [US3] Définir le dictionnaire de ressources `DofusTheme`
      (palette de couleurs, style de panneau à coins arrondis, brosse de bordure
      dorée/bronze) dans `src/Pofus.Hud/Styles/DofusTheme.xaml`
- [x] T029 [US3] Définir le style d'icône ronde pour les emplacements de module
      dans `src/Pofus.Hud/Styles/DofusTheme.xaml` (dépend de T028, T027)
- [x] T030 [US3] Appliquer `DofusTheme` à `HudWindow` et `ModuleSlotView` dans
      `src/Pofus.Hud/HudWindow.xaml`, `src/Pofus.Hud/Controls/ModuleSlotView.xaml`
      (dépend de T028, T029, T022, T023)

**Checkpoint**: Les 3 user stories sont fonctionnelles indépendamment — style
visuel (panneau sombre, bordure dorée, slots ronds) validé visuellement en
conditions réelles (voir T033) ; seules les icônes réelles manquent (T027)

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Améliorations transverses aux 3 user stories

- [x] T031 [P] Implémenter `ModuleHost` : capture + log explicite de toute
      exception non gérée levée par un `IHudModule` (`CreateContent`,
      `OnActivated`, `OnDeactivated`), bascule du slot en état "vide" plutôt que
      crash de l'application, per
      [contracts/module-contract.md § Garanties fournies par le HUD](contracts/module-contract.md)
      dans `src/Pofus.Hud/ModuleHost.cs`
- [x] T032 [P] Documenter le build/lancement de `Pofus.App` dans `README.md` à la
      racine du dépôt
- [x] T033 Exécuter l'ensemble des scénarios de
      [quickstart.md](quickstart.md) de bout en bout et consigner les résultats —
      **résultats** : build + 13/13 tests unitaires au vert ; lancement réel
      validé à 3 reprises, dont une fois avec une vraie fenêtre Dofus ouverte sur
      la machine de test (détection correcte "1 compte(s) Dofus détecté(s)") et
      une fois sans (état "Aucun compte Dofus détecté" correct) ; HUD visible
      au-dessus d'autres fenêtres (VSCode, navigateur) ; 5 emplacements de
      modules affichés avec tooltip ; position restaurée à l'identique sur 2
      lancements consécutifs après correctif de dérive (voir HudWindow.xaml.cs
      OnLoaded) ; icône systray enregistrée sans erreur. Non exécuté faute de
      matériel : test avec 4-8 fenêtres Dofus simultanées (un seul compte
      disponible sur la machine de test) ; comparaison visuelle formelle à 90%
      (SC-003) sans les vraies icônes (T027 différé)
- [x] T034 [P] Audit "pas de `catch` silencieux" (Principe I) sur
      `src/Pofus.Platform/` et `src/Pofus.Hud/` — vérifier qu'aucune exception
      n'est avalée sans log explicite — **résultat** : 10 blocs `catch` dans le
      code source, tous journalisent explicitement avant de gérer l'erreur ;
      aucun `catch` vide ou silencieux trouvé

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Aucune dépendance — démarre immédiatement
- **Foundational (Phase 2)**: Dépend de Setup — **bloque** les 3 user stories
- **User Story 1 (Phase 3)**: Dépend de Foundational uniquement
- **User Story 2 (Phase 4)**: Dépend de Foundational ; réutilise `HudWindow` créé
  en US1 (T015, T020) mais reste testable indépendamment de la logique US1
  (always-on-top/multi-fenêtres)
- **User Story 3 (Phase 5)**: Dépend de Foundational ; s'applique aux contrôles
  créés en US1/US2 (T022, T023) mais n'introduit aucune nouvelle logique
  fonctionnelle — purement visuel
- **Polish (Phase 6)**: Dépend de la complétion des user stories souhaitées

### User Story Dependencies

- **US1 (P1)**: Aucune dépendance sur US2/US3 — livrable seule (MVP)
- **US2 (P2)**: Réutilise la fenêtre créée par US1 (partage de fichier
  `HudWindow.xaml.cs`) mais sa valeur (emplacements + persistance) est
  indépendamment testable
- **US3 (P3)**: Purement additive (styles visuels) sur les contrôles de
  US1/US2 — aucune nouvelle capacité fonctionnelle

### Within Each User Story

- Tests (si présents) avant l'implémentation correspondante
- Modèles/services de `Pofus.Core`/`Pofus.Platform` avant les contrôles WPF qui
  les consomment
- `HudWindow` de base (US1) avant l'ajout des emplacements (US2) et du thème
  (US3)

### Parallel Opportunities

- T002–T005 (Setup) en parallèle
- T006, T007, T009, T010, T011 (Foundational, fichiers distincts) en parallèle ;
  T008 et T012 dépendent respectivement de T007 et d'aucun autre (peut démarrer
  tôt)
- T013, T014 (tests US1) en parallèle entre eux et avec T009–T012 s'ils sont déjà
  posés
- T021 (test US2) en parallèle avec la fin de US1
- T027, T028 (US3) en parallèle
- US2 et US3 peuvent être menées en parallèle par deux personnes une fois US1
  livrée (toutes deux ne font qu'étendre `HudWindow`/`ModuleSlotView`, à
  coordonner sur ces fichiers partagés)

---

## Parallel Example: User Story 1

```bash
# Lancer les tests de la User Story 1 ensemble :
Task: "Tests unitaires DofusWindowLocator dans tests/Pofus.Platform.Tests/DofusWindowLocatorTests.cs"
Task: "Tests unitaires TopmostWindowController dans tests/Pofus.Platform.Tests/TopmostWindowControllerTests.cs"

# Lancer les services Foundational réutilisés par US1 ensemble :
Task: "DofusWindowLocator dans src/Pofus.Platform/DofusWindowLocator.cs"
Task: "TopmostWindowController dans src/Pofus.Platform/TopmostWindowController.cs"
Task: "ForegroundWindowWatcher dans src/Pofus.Platform/ForegroundWindowWatcher.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Compléter Phase 1 : Setup
2. Compléter Phase 2 : Foundational (bloquant)
3. Compléter Phase 3 : User Story 1
4. **STOP et VALIDER** : exécuter quickstart.md § 1, 2, 5 (HUD unique
   au-dessus de toutes les fenêtres, affichage/masquage) indépendamment
5. Démontrer si prêt — le HUD fonctionne déjà comme socle, même sans
   emplacements de modules stylés

### Incremental Delivery

1. Setup + Foundational → socle prêt
2. US1 → tester indépendamment → HUD always-on-top multi-fenêtres fonctionnel
   (MVP)
3. US2 → tester indépendamment → emplacements + persistance de disposition
4. US3 → tester indépendamment → esthétique Dofus complète
5. Chaque story ajoute de la valeur sans casser les précédentes

---

## Notes

- [P] = fichiers différents, pas de dépendance
- Chaque user story est indépendamment complétable et testable
- Vérifier que les tests échouent avant l'implémentation correspondante
- Commit après chaque tâche ou groupe logique
- S'arrêter à chaque checkpoint pour valider la story indépendamment
- Éviter : tâches vagues, conflits sur un même fichier non coordonnés,
  dépendances inter-stories qui casseraient l'indépendance
