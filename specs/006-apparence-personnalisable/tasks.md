---

description: "Task list template for feature implementation"
---

# Tasks: Apparence personnalisable

**Input**: Design documents from `/specs/006-apparence-personnalisable/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md)

**Tests**: xUnit sur la logique pure `Pofus.Core.Appearance` (analyse
hexadécimale, dérivation des nuances, plancher d'opacité, exclusion des
couleurs fonctionnelles). Le rendu est validé manuellement via
[quickstart.md](quickstart.md).

## Path Conventions

```text
src/Pofus.Core/Appearance/      (nouveau)
src/Pofus.Hud/Appearance/       (nouveau)
src/Pofus.Hud/Modules/Settings/ (extension)
src/Pofus.Hud/Styles/PofusTheme.xaml + tous les .xaml consommateurs
tests/Pofus.Core.Tests/Appearance/ (nouveau)
```

---

## Phase 1: Setup

- [x] T001 Créer `src/Pofus.Core/Appearance/`, `src/Pofus.Hud/Appearance/`,
      `tests/Pofus.Core.Tests/Appearance/`

---

## Phase 2: Foundational

- [x] T002 [P] Définir `ThemeColor` (analyse/format hexadécimal,
      `Lighten`/`Darken` saturants) dans
      `src/Pofus.Core/Appearance/ThemeColor.cs`
- [x] T003 [P] Définir `AppearancePreferences` (3 couleurs, 2 opacités,
      `MinOpacity`, `ClampOpacities`, `Clone`) dans
      `src/Pofus.Core/Appearance/AppearancePreferences.cs`
- [x] T004 Définir `ThemePalette.Build` : les 3 réglages → les 8 clés de
      thème, en **excluant** `Pofus.Accent` et `Pofus.Danger` (repères
      fonctionnels) dans `src/Pofus.Core/Appearance/ThemePalette.cs`
      (dépend de T002, T003)
- [x] T005 [P] Définir les préréglages Sombre / Verre translucide /
      Contrasté, chacun rendu en instance neuve, dans
      `src/Pofus.Core/Appearance/AppearancePresets.cs`
- [x] T006 Implémenter `IAppearanceStore`/`AppearanceStore`
      (`appearance.json`, défauts journalisés, `ClampOpacities` au
      chargement pour qu'un fichier édité à la main ne contourne pas le
      plancher) dans `src/Pofus.Core/Appearance/AppearanceStore.cs`
- [x] T007 **Correctif de conception** : convertir les 8 clés thémées de
      `StaticResource` en `DynamicResource` dans tous les `.xaml`. WPF gèle
      les `Freezable` chargés depuis un `ResourceDictionary` : la mutation en
      place prévue au plan était impossible (constat à l'exécution). Les
      clés non thémées (`Pofus.Accent`, styles, rayons) restent en
      `StaticResource`.
- [x] T008 Implémenter `ThemeApplier` : **remplace** les entrées de ressource
      (pinceaux figés, plus rapides à rendre) plutôt que de les muter, une
      clé absente est journalisée et ignorée, dans
      `src/Pofus.Hud/Appearance/ThemeApplier.cs` (dépend de T004, T007)

---

## Phase 3: User Story 1 & 2 - Transparence et couleurs (Priority: P1) 🎯 MVP

- [x] T009 [US1][US2] Ajouter la section « Apparence » à `SettingsWindow` :
      3 lignes (fond, texte, bordures) avec pastille, champ hexadécimal et
      curseur d'opacité, dans
      `src/Pofus.Hud/Modules/Settings/SettingsWindow.xaml`
- [x] T010 [US1][US2] Câbler l'aperçu direct : chaque modification applique
      le thème immédiatement, affiche l'état et programme la persistance
      débouncée (400 ms), dans `SettingsWindow.xaml.cs` (dépend de T008)
- [x] T011 [US1][US2] Rejeter explicitement une couleur hexadécimale
      invalide (message + restauration de la valeur en vigueur), jamais
      d'application silencieuse
- [x] T012 [US1][US2] Appliquer l'apparence au démarrage **avant** toute
      création de fenêtre, pour qu'aucune n'apparaisse d'abord dans le thème
      par défaut, dans `src/Pofus.App/App.xaml.cs`
- [x] T013 **Correctif** : assigner les champs *avant* `InitializeComponent`
      et neutraliser les événements pendant, car les curseurs déclarent
      `Minimum="0.15"` et WPF lève `ValueChanged` dès l'analyse du XAML —
      d'où un `NullReferenceException` au démarrage (constat à l'exécution)

---

## Phase 4: User Story 3 - Préréglages (Priority: P2)

- [x] T014 [US3] Générer les boutons de préréglage et les appliquer en une
      action, ajustables ensuite, dans `SettingsWindow.xaml.cs` (dépend de
      T005, T010)
- [x] T015 [US3] Bouton « Réinitialiser l'apparence » (FR-008)

---

## Phase 5: Polish

- [x] T016 [P] Tests unitaires `ThemeColor`, `AppearancePreferences`,
      `ThemePalette` (20 tests) dans
      `tests/Pofus.Core.Tests/Appearance/AppearanceTests.cs`
- [x] T017 [P] Audit « pas de `catch` silencieux » (Principe I)
- [x] T018 Non-régression des features 001-005 (build + tests au vert)
- [x] T019 Exécuter les scénarios de [quickstart.md](quickstart.md) et
      consigner les résultats — **résultats** : build propre, **94/94 tests
      au vert** (80 `Pofus.Core.Tests` dont 20 nouveaux, 14
      `Pofus.Platform.Tests`). Validé en conditions réelles : ouverture des
      réglages depuis le HUD, application du préréglage « Verre translucide »
      via UI Automation → **le HUD, le widget et la fenêtre de réglages
      elle-même deviennent translucides instantanément** (captures avant/après
      comparées, le décor de Dofus transparaît), et `appearance.json` reflète
      exactement le préréglage. Aucun avertissement de thème après le
      correctif T007. Non validé automatiquement : la lecture fine des
      champs hexadécimaux et le comportement au plancher d'opacité — à
      vérifier à la main via quickstart §3 et §5.

---

## Notes

- Deux défauts n'ont été révélés que par l'exécution, pas par la relecture :
  T007 (les pinceaux d'un `ResourceDictionary` sont figés — l'hypothèse
  centrale du plan était fausse) et T013 (`ValueChanged` levé pendant
  `InitializeComponent`). Le plan a été corrigé en conséquence.
- `Pofus.SurfaceHover` figurait dans le modèle initial mais n'existe pas
  comme pinceau dans le thème (seulement comme `Color` pour les animations) :
  retiré de la palette. Les fonds de survol ne suivent donc pas encore le
  thème — piste d'amélioration si le besoin se fait sentir.
