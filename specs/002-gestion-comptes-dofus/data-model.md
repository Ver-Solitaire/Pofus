# Data Model: Gestion des Comptes Dofus

## AccountSession

Vue en mémoire d'un compte, reconstruite à chaque rafraîchissement en combinant
la détection brute (`IDofusWindowLocator`) et les préférences persistées.

| Champ | Type | Description | Règles |
|---|---|---|---|
| `Pseudo` | `string` | Identifiant stable du compte | Clé de toutes les préférences persistées (FR-005, FR-008, FR-009) |
| `ClassName` | `string` | Classe du personnage | `"Classe inconnue"` si non déterminable (FR-003) |
| `WindowHandle` | `nint` | Handle Win32 de la fenêtre actuelle | Change à chaque reconnexion — jamais utilisé comme clé de persistance |
| `IsActive` | `bool` | Participe aux futures actions groupées | Persisté par pseudo (FR-005) |
| `Team` | `string` | Équipe assignée | Défaut `"Équipe 1"` si non assigné (FR-006) |
| `IsLeader` | `bool` | Ce compte est-il le leader désigné | Au plus un `true` à la fois (FR-007) |

## AccountPreferences (persisté)

Fichier `%APPDATA%\Pofus\account-preferences.json`.

| Champ | Type | Description | Règles |
|---|---|---|---|
| `ActiveByPseudo` | `Dictionary<string, bool>` | État actif/inactif par pseudo | Défaut `true` si absent |
| `TeamByPseudo` | `Dictionary<string, string>` | Équipe par pseudo | Défaut `"Équipe 1"` si absent |
| `LeaderPseudo` | `string?` | Pseudo du leader désigné | Conservé même si le compte est déconnecté (FR-008) |
| `CustomOrder` | `List<string>` | Ordre des pseudos, le plus ancien en tête | Plafonné à 50 entrées ; purge des entrées non détectées les plus anciennes en priorité (FR-010) |

## Relations

```text
AccountPreferences 1 ── * (ActiveByPseudo, TeamByPseudo entries)
AccountPreferences 1 ── 0..1 LeaderPseudo
DofusWindowInfo (feature 001, Pofus.Platform) ──[AccountTitleParser]──> AccountSession
AccountSession * ──[merge avec]── AccountPreferences ──> vue affichée
```

- `AccountSession` est une projection éphémère : jamais persistée telle
  quelle, reconstruite à chaque rafraîchissement à partir de la détection brute
  + `AccountPreferences`.
- `AccountPreferences` est la seule source persistée — indexée par `Pseudo`,
  jamais par `WindowHandle` (qui change à chaque reconnexion).

## State Transitions

- **Détection** : une fenêtre Dofus valide (titre ≠ vide, ne commence pas par
  "dofus") apparaît → `AccountTitleParser` en extrait `Pseudo`/`ClassName` →
  fusion avec `AccountPreferences` existantes (ou valeurs par défaut si
  premier pseudo vu) → nouvelle entrée dans `CustomOrder` si absente.
- **Perte de fenêtre** : le compte disparaît de la liste affichée, mais ses
  préférences (`ActiveByPseudo`, `TeamByPseudo`, position dans `CustomOrder`)
  restent en base jusqu'à purge éventuelle (FR-010).
- **Leader** : `LeaderPseudo` ne change que sur action explicite de
  l'utilisateur ; la déconnexion du leader ne réinitialise jamais
  `LeaderPseudo` (FR-008, Edge Cases).
