# Implementation Plan: Apparence personnalisable

**Branch**: `006-apparence-personnalisable` | **Date**: 2026-08-19 | **Spec**: [spec.md](spec.md)

## Summary

Un jeu de réglages global (fond couleur+opacité, texte couleur, bordures
couleur+opacité) piloté depuis la fenêtre Réglages existante, appliqué en
direct à toutes les fenêtres et persisté. Le point d'appui technique : toutes
les fenêtres consomment déjà les mêmes `SolidColorBrush` du dictionnaire de
thème via `{StaticResource}` ; **muter la `Color` de ces pinceaux partagés
repeint instantanément toute l'application**, sans toucher au moindre XAML
existant ni migrer vers `DynamicResource`.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (aucune nouvelle dépendance)

**Primary Dependencies**: WPF uniquement

**Storage**: `%APPDATA%\Pofus\appearance.json`, même pattern que les stores
existants

**Testing**: xUnit pour la logique pure (`AppearancePreferences`, dérivation
des teintes, plancher d'opacité, persistance). Le rendu est validé
manuellement via [quickstart.md](quickstart.md).

**Target Platform**: Windows 10/11

**Project Type**: desktop-app — extension des projets existants

**Performance Goals**: changement visible en moins d'une seconde (SC-001) —
en pratique immédiat, muter une `Color` déclenche un simple repaint

**Constraints**: Principe II (aucun blocage du thread UI) ; l'écriture du
fichier est débouncée pour ne pas écrire à chaque cran de curseur

**Scale/Scope**: 3 éléments réglables, 3 préréglages, ~10 clés de thème
recalculées

## Constitution Check

- **I. Robustesse** : fichier absent/corrompu → défauts journalisés
  explicitement (FR-010), jamais d'exception silencieuse. PASS.
- **II. Fluidité** : mutation de couleur = repaint, pas de reconstruction
  d'arbre visuel ; persistance débouncée hors du chemin interactif. PASS.
- **III. Séparation** : le modèle et les règles de dérivation vivent dans
  `Pofus.Core.Appearance` (testables sans WPF) ; seule l'application des
  couleurs aux pinceaux vit dans `Pofus.Hud`. PASS.
- **IV. Intégration Windows** : rien de nouveau côté Win32. PASS.
- **V. Données locales** : persistance locale, aucun réseau. PASS.

Aucune violation.

## Key Design Decision — pourquoi muter les pinceaux

`PofusTheme.xaml` définit des `SolidColorBrush` nommés
(`Pofus.Surface`, `Pofus.TextPrimary`, `Pofus.BorderSubtle`…), fusionnés dans
les ressources de l'`Application`. Aucun n'est figé (`Freeze`), et
`{StaticResource}` résout **une référence vers l'objet**, pas une copie :
changer `brush.Color` se propage donc à tous les consommateurs, y compris à
l'intérieur des `ControlTemplate`, et aux fenêtres déjà ouvertes (FR-005,
edge case « fenêtres ouvertes »).

**Alternatives écartées** :
- Migrer tout le XAML vers `DynamicResource` et remplacer les ressources :
  fonctionne aussi, mais demande de toucher ~60 usages répartis dans tous les
  fichiers, pour un résultat identique.
- Régénérer et réinjecter le `ResourceDictionary` entier : les fenêtres déjà
  ouvertes ne reprendraient pas les `StaticResource` déjà résolues.

**Conséquence à respecter** : ne jamais appeler `Freeze()` sur les pinceaux du
thème (les pinceaux locaux créés ailleurs, comme l'icône du widget, peuvent
l'être — ils ne font pas partie du thème).

## Mapping réglage → clés de thème

| Réglage utilisateur | Clés repeintes |
|---|---|
| Fond (couleur + opacité) | `Pofus.Bg`, `Pofus.Surface`, `Pofus.SurfaceRaised`, `Pofus.SurfaceHover` — nuances dérivées par éclaircissement progressif, alpha = opacité |
| Texte (couleur) | `Pofus.TextPrimary`, `Pofus.TextMuted` (alpha réduit), `Pofus.TextDisabled` (alpha plus réduit) |
| Bordures (couleur + opacité) | `Pofus.BorderSubtle`, `Pofus.BorderStrong` (plus contrastée), alpha = opacité |

`Pofus.Accent` et `Pofus.Danger` **ne sont pas** soumis au thème : ils portent
un sens fonctionnel (leader, focus, erreur) qui doit rester lisible quel que
soit le réglage (spec.md Assumptions).

## Project Structure

```text
specs/006-apparence-personnalisable/
├── plan.md · spec.md · data-model.md · quickstart.md · tasks.md
└── checklists/requirements.md

src/Pofus.Core/Appearance/
├── AppearancePreferences.cs      # modèle + plancher d'opacité + dérivations
├── AppearancePresets.cs          # Sombre / Verre / Contrasté
└── AppearanceStore.cs            # appearance.json

src/Pofus.Hud/Appearance/
└── ThemeApplier.cs               # applique le modèle aux SolidColorBrush du thème

src/Pofus.Hud/Modules/Settings/
└── SettingsWindow.xaml(.cs)      # section « Apparence » (extension)

tests/Pofus.Core.Tests/Appearance/
```

**Structure Decision**: extension des projets existants, aucun nouveau
`.csproj`. Le modèle reste dans `Pofus.Core` (donc testable sans WPF, y
compris les dérivations de teinte, qui sont de l'arithmétique sur des
composantes ARGB) ; `ThemeApplier` est le seul point qui connaît WPF.
