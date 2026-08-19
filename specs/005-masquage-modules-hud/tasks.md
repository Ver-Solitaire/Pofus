---

description: "Task list template for feature implementation"
---

# Tasks: Masquage des fenêtres de Pofus

**Input**: Design documents from `/specs/005-masquage-modules-hud/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md)

> **Révision du 2026-08-19** — une première liste de tâches visait le masquage
> des *boutons de module dans la barre HUD*. Le besoin réel étant le masquage
> des *fenêtres*, cette liste la remplace. Ce qui a survécu de l'itération
> précédente (généralisation du `GlobalHotkeyListener`, styles de menu
> contextuel, correctif topmost) est marqué comme tel.

**Tests**: xUnit pour la logique pure `Pofus.Core` (`PanelPreferences`,
persistance). Le comportement `RegisterHotKey`/`WM_HOTKEY` et le rendu des
menus contextuels sont validés manuellement via [quickstart.md](quickstart.md).

**Organization**: Tâches groupées par user story (US1/US2/US3 de spec.md).

## Path Conventions

Extension des projets existants — aucun nouveau `.csproj` :

```text
src/Pofus.Core/Panels/            (nouveau)
src/Pofus.Hud/Panels/             (nouveau)
src/Pofus.Hud/Styles/PofusTheme.xaml
src/Pofus.Hud/Modules/Navigation/ (GlobalHotkeyListener, NavigationShortcutsWindow)
src/Pofus.App/                    (App.xaml.cs, TrayIconController.cs)
tests/Pofus.Core.Tests/Panels/    (nouveau)
```

---

## Phase 1: Setup

- [x] T001 Créer `src/Pofus.Core/Panels/`, `src/Pofus.Hud/Panels/`,
      `tests/Pofus.Core.Tests/Panels/`

---

## Phase 2: Foundational (Blocking Prerequisites)

- [x] T002 [P] Définir `PanelSettings` (`IsHidden`, `ShowShortcut`) et
      `PanelPreferences` (`For`, `FindShortcutConflict`) dans
      `src/Pofus.Core/Panels/PanelPreferences.cs` per data-model.md
- [x] T003 Implémenter `IPanelPreferencesStore`/`PanelPreferencesStore`
      (JSON, `%APPDATA%\Pofus\panels.json`, défauts journalisés, jamais
      d'exception silencieuse) dans
      `src/Pofus.Core/Panels/PanelPreferencesStore.cs` (dépend de T002)
- [x] T004 Généraliser `GlobalHotkeyListener` : clé d'action `string`,
      identifiants `RegisterHotKey` attribués par compteur interne (plus de
      cast d'enum), `event Action<string>` — *hérité de l'itération
      précédente, conservé tel quel* dans
      `src/Pofus.Hud/Modules/Navigation/GlobalHotkeyListener.cs`
- [x] T005 Adapter `NavigationHudModule`/`NavigationShortcutsWindow` à la
      clé `string` via `NavigationActionIds` (`"nav:*"`), comportement
      utilisateur inchangé — *hérité*
- [x] T006 Ajouter les styles `ContextMenu`/`MenuItem` au thème pour que les
      menus de masquage ne retombent pas sur le chrome Windows par défaut
      dans `src/Pofus.Hud/Styles/PofusTheme.xaml`
- [x] T007 **Correctif** : ne plus réaffirmer `HWND_TOPMOST` quand la fenêtre
      passée au premier plan appartient à notre propre processus — sans quoi
      le HUD et le widget recouvrent leurs propres popups, rendant tout menu
      contextuel invisible. Dans `src/Pofus.Hud/HudWindow.xaml.cs` et
      `src/Pofus.Hud/Switcher/AccountSwitcherWidget.xaml.cs`
- [x] T008 Vérifier la non-régression des raccourcis de navigation
      (build + `dotnet test` au vert)

**Checkpoint**: Socle prêt

---

## Phase 3: User Story 1 - Masquer une fenêtre (Priority: P1) 🎯 MVP

- [x] T009 [US1] Implémenter `HideablePanel` + `PanelVisibilityService`
      (`Register`, `Hide`, `Show`, `IsHidden`, `GetPanels`, `LoadAsync`,
      `ApplyPersistedState`) dans
      `src/Pofus.Hud/Panels/PanelVisibilityService.cs` (dépend de T003)
- [x] T010 [US1] Implémenter `PanelContextMenu.Attach(window, panelId,
      service)` — menu « Masquer cette fenêtre » générique, réutilisable pour
      toute fenêtre future (FR-012) dans
      `src/Pofus.Hud/Panels/PanelContextMenu.cs` (dépend de T009)
- [x] T011 [US1] Déclarer les deux fenêtres masquables (`"hud"`,
      `"switcher"`) et attacher leur menu contextuel dans
      `src/Pofus.App/App.xaml.cs` (dépend de T009, T010)
- [x] T012 [US1] Retirer l'ancien masquage de slots : `ModuleSlot.IsHidden`,
      le menu contextuel de `ModuleSlotView`, le filtrage de rendu et le
      placeholder « tous masqués » du HUD (périmètre corrigé)

**Checkpoint**: US1 fonctionnelle (MVP)

---

## Phase 4: User Story 2 - Réafficher par raccourci (Priority: P1) 🎯 MVP

- [x] T013 [P] [US2] Tests unitaires `PanelPreferencesStore` (round-trip
      état + raccourci, fichier absent, JSON corrompu journalisé) dans
      `tests/Pofus.Core.Tests/Panels/PanelPreferencesStoreTests.cs`
- [x] T014 [US2] Implémenter `AttachShortcuts` + routage `"panel:*"` →
      `Show`, idempotent, dans
      `src/Pofus.Hud/Panels/PanelVisibilityService.cs` (dépend de T009)
- [x] T015 [US2] **Correctif d'ordonnancement** : charger les préférences
      (`LoadAsync`) *avant* `Show()` de la fenêtre hôte, car `AttachShortcuts`
      s'exécute depuis son événement `Loaded` — sans quoi aucun raccourci
      n'était enregistré. Séparer `LoadAsync` de `ApplyPersistedState` dans
      `PanelVisibilityService.cs` et `src/Pofus.App/App.xaml.cs`
- [x] T016 [US2] Remplacer le menu de la zone de notification par la liste
      des fenêtres masquables avec leur état (point d'entrée garanti, FR-009)
      dans `src/Pofus.App/TrayIconController.cs`

**Checkpoint**: US1 + US2 — le masquage est réversible (MVP complet)

---

## Phase 5: User Story 3 - Configurer le raccourci par fenêtre (Priority: P2)

- [x] T017 [P] [US3] Tests unitaires `PanelPreferences` (`For` crée à la
      volée, `FindShortcutConflict` détecte/ignore correctement) dans
      `tests/Pofus.Core.Tests/Panels/PanelPreferencesTests.cs`
- [x] T018 [US3] Ajouter la section « Réafficher une fenêtre » (une ligne par
      fenêtre : libellé, liaison, « Modifier », et « Réafficher » si masquée)
      dans `src/Pofus.Hud/Modules/Navigation/NavigationShortcutsWindow.xaml`
      + `.xaml.cs`
- [x] T019 [US3] Détection de conflit croisée navigation ↔ fenêtres avant
      toute application (FR-006), message nommant le raccourci en conflit
- [x] T020 [US3] **Correctif** : suspendre les hotkeys globaux pendant la
      capture (`SuspendAll`/`ResumeAll`) — sans quoi une combinaison déjà
      enregistrée est interceptée par Windows, la capture reste bloquée et
      l'action liée se déclenche à la place, rendant le conflit impossible à
      signaler. Dans `GlobalHotkeyListener.cs` et
      `NavigationShortcutsWindow.xaml.cs`

**Checkpoint**: les 3 user stories sont fonctionnelles

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T021 [P] Audit « pas de `catch` silencieux » (Principe I) sur les
      fichiers nouveaux/modifiés
- [x] T022 Non-régression complète des features 001-004
      (build + `dotnet test` au vert sur toute la solution)
- [x] T023 Exécuter les scénarios de [quickstart.md](quickstart.md) et
      consigner les résultats — **résultats** : build propre, 74/74 tests au
      vert (60 `Pofus.Core.Tests` dont 9 nouveaux, 14 `Pofus.Platform.Tests`).
      Validé en conditions réelles avec de vraies fenêtres Dofus :
      clic droit → « Masquer cette fenêtre » sur le widget des personnages
      (menu trouvé et invoqué via UI Automation, widget masqué) ; persistance
      confirmée dans `panels.json` puis **au redémarrage** (widget toujours
      masqué) ; attribution de `Ctrl+Alt+A` au widget et `Ctrl+Alt+B` à la
      barre HUD depuis l'écran des raccourcis ; **conflit croisé signalé**
      (« Combinaison déjà utilisée par « réafficher Barre HUD » ») sans
      modification de la liaison existante ; **réaffichage par raccourci
      global validé depuis une fenêtre Dofus au premier plan**. Aucun échec
      d'enregistrement dans les logs. Non validé automatiquement : le menu de
      la zone de notification (WinForms `NotifyIcon`, hors portée de
      l'automation utilisée ici) — à vérifier à la main. Les raccourcis de
      test ont été retirés (`panels.json` supprimé) pour ne pas laisser de
      liaisons non demandées.

---

## Notes

- [P] = fichiers différents, pas de dépendance
- Trois correctifs de fond ont été trouvés *par la validation en conditions
  réelles*, pas par la relecture : T007 (popups recouverts), T015 (ordre de
  chargement des préférences), T020 (hotkeys interceptant la capture).
