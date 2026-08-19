# Contract: Module → HUD

Ce contrat définit l'interface que toute fonctionnalité future ("module" —
gestion de comptes, macros, menu radial, etc.) DOIT implémenter pour pouvoir être
hébergée dans un `ModuleSlot` du HUD. Cette fonctionnalité (le HUD) fournit le
contenant ; elle ne fournit aucune implémentation de ce contrat.

## Interface `IHudModule`

| Membre | Type | Description | Règles |
|---|---|---|---|
| `ModuleId` | `string` (lecture seule) | Identifiant stable et unique du module | Doit correspondre à un `SlotId` de disposition connu du HUD |
| `DisplayName` | `string` (lecture seule) | Nom affiché en infobulle/accessibilité | Non vide |
| `IconResource` | `Uri` (lecture seule) | Icône ronde conforme au style visuel de référence | Voir FR-004 (esthétique Dofus) |
| `IsAvailable` | `bool` (lecture seule) | Le module peut-il s'afficher dans son état courant | Si `false`, le slot DOIT rester dans l'état visuel "vide" (FR-007) |
| `CreateContent()` | `UIElement` | Retourne le contenu visuel du module à afficher dans le slot | DOIT être un appel non bloquant ; toute initialisation lourde est asynchrone |
| `OnActivated()` | `void` | Appelé quand le HUD affiche effectivement ce module | Ne DOIT jamais lever d'exception non gérée (Principe I) |
| `OnDeactivated()` | `void` | Appelé quand le module est masqué/retiré du HUD | Doit libérer toute ressource acquise dans `OnActivated` |

## Garanties fournies par le HUD (côté contenant)

- Le HUD appelle `CreateContent()` au plus une fois par cycle d'affichage du
  module ; il ne recrée pas le contenu à chaque frame.
- Toute exception non interceptée levée par un module (`CreateContent`,
  `OnActivated`, `OnDeactivated`) est capturée par le HUD, journalisée
  explicitement (Principe I — pas de `catch` silencieux), et le slot concerné
  bascule en état visuel "vide" plutôt que de faire planter l'application.
- Le HUD garantit que `OnActivated`/`OnDeactivated` s'exécutent hors du thread
  d'interface pour toute opération potentiellement bloquante déclenchée par le
  module (Principe II).

## Hors périmètre de cette fonctionnalité

- Aucune implémentation concrète de `IHudModule` n'est livrée par cette
  fonctionnalité — les emplacements restent à l'état `Empty` (FR-007) tant
  qu'aucun module réel n'est développé dans une fonctionnalité ultérieure.
- La découverte/chargement dynamique de modules (plugin loading) n'est pas
  couverte ici ; le nombre et l'ordre des `ModuleSlot` sont fixes pour cette
  version (FR-008).
