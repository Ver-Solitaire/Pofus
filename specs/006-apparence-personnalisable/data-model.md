# Data Model: Apparence personnalisable

## ThemeColor (nouveau — `Pofus.Core.Appearance`)

Une couleur indépendante de WPF, pour que le modèle et ses dérivations
restent testables sans Windows (Principe III) :

```csharp
public readonly record struct ThemeColor(byte R, byte G, byte B)
{
    public static ThemeColor? TryParseHex(string text);  // "#RRGGBB" ou "RRGGBB"
    public string ToHex();
}
```

## AppearancePreferences (nouveau)

```csharp
public sealed class AppearancePreferences
{
    public ThemeColor Background { get; set; }
    public double BackgroundOpacity { get; set; }   // 0..1, plancher appliqué
    public ThemeColor Text { get; set; }
    public ThemeColor Border { get; set; }
    public double BorderOpacity { get; set; }       // 0..1, plancher appliqué

    public const double MinOpacity = 0.15;          // FR-009
    public static AppearancePreferences CreateDefault();
    public void ClampOpacities();
}
```

- `MinOpacity` empêche de rendre une fenêtre invisible (FR-009).
  `ClampOpacities()` est appliqué au chargement **et** à chaque modification,
  pour qu'un fichier édité à la main ne puisse pas contourner le plancher.
- Les valeurs par défaut reproduisent exactement le thème actuel, pour qu'un
  utilisateur qui n'ouvre jamais ces réglages ne voie aucun changement.

## AppearancePresets (nouveau)

```csharp
public static class AppearancePresets
{
    public static IReadOnlyList<AppearancePreset> All { get; }
}
public sealed record AppearancePreset(string Name, AppearancePreferences Values);
```

| Préréglage | Intention |
|---|---|
| **Sombre** | l'apparence actuelle, opaque — le défaut |
| **Verre translucide** | fond très transparent, bordures claires — pour lire le jeu derrière |
| **Contrasté** | fond opaque, texte et bordures très lisibles |

## Dérivation des nuances (logique pure, testée)

À partir des trois couleurs réglées, les ~9 clés du thème sont recalculées :

| Clé | Règle |
|---|---|
| `Pofus.Bg` | fond, assombri d'un cran |
| `Pofus.Surface` | fond tel quel |
| `Pofus.SurfaceRaised` | fond éclairci d'un cran |
| `Pofus.SurfaceHover` | fond éclairci de deux crans |
| *(les quatre ci-dessus)* | alpha = `BackgroundOpacity` |
| `Pofus.TextPrimary` | texte tel quel, opaque |
| `Pofus.TextMuted` | texte à 65% d'alpha |
| `Pofus.TextDisabled` | texte à 40% d'alpha |
| `Pofus.BorderSubtle` | bordure telle quelle, alpha = `BorderOpacity` |
| `Pofus.BorderStrong` | bordure éclaircie d'un cran, alpha = `BorderOpacity` |

`Pofus.Accent`, `Pofus.Accent.Color` et `Pofus.Danger` sont **exclus** : ce
sont des repères fonctionnels (leader, focus, erreur).

## Persistance

`IAppearanceStore` → `%APPDATA%\Pofus\appearance.json`, même pattern que les
stores existants : fichier absent → défauts journalisés en INFO ; JSON
corrompu → défauts journalisés en ERROR ; jamais d'exception silencieuse
(FR-010).

L'écriture est **débouncée** (~400 ms) : bouger un curseur produit des
dizaines de changements, on ne veut pas autant d'écritures disque.

## ThemeApplier (nouveau — `Pofus.Hud.Appearance`)

Seul composant qui connaît WPF :

```csharp
public sealed class ThemeApplier
{
    public ThemeApplier(ResourceDictionary themeResources, IAppLogger logger);
    public void Apply(AppearancePreferences preferences);
}
```

`Apply` récupère chaque `SolidColorBrush` par sa clé et affecte sa `Color`.
Les pinceaux du thème ne doivent jamais être figés (`Freeze`), sous peine de
lever une exception ici — voir plan.md « Key Design Decision ». Une clé
absente ou d'un type inattendu est journalisée et ignorée, sans interrompre
l'application des autres.
