# Data Model: Paramètres — Détection de Conflits et Démarrage

## AppPreferences (persisté)

Fichier `%APPDATA%\Pofus\app-preferences.json`.

| Champ | Type | Description | Règles |
|---|---|---|---|
| `IgnoreConflictWarning` | `bool` | L'utilisateur a choisi de ne plus être averti | Défaut `false` ; passe à `true` via FR-005, revient à `false` via FR-009 (US3) |
| `LaunchAtStartup` | `bool` | Reflète l'état voulu du lancement automatique | Défaut `false` ; la source de vérité réelle reste la clé de registre (FR-008), ce champ sert d'état affiché dans les réglages |

## KnownConflictingSoftware (donnée statique)

| Champ | Type | Description |
|---|---|---|
| `ProcessNames` | `IReadOnlyList<string>` | Noms de processus recherchés au démarrage (initialement `["organizer"]`) |

## Relations

```text
AppPreferencesStore ──> AppPreferences (JSON local)
ConflictingSoftwareDetector ──[utilise]──> KnownConflictingSoftware.ProcessNames
                             ──[interroge]──> IProcessController (Pofus.Platform)
StartupRegistration ──[lit/écrit]──> clé de registre Run (Pofus.Platform)
```

- `AppPreferences` est la seule donnée persistée par ce module ; `KnownConflictingSoftware`
  est une constante de code, pas une donnée utilisateur.
- L'état réel du lancement au démarrage vit dans le registre Windows (source de
  vérité), pas uniquement dans `AppPreferences` — au chargement des réglages,
  l'état affiché DOIT refléter la clé de registre réelle plutôt que la seule
  valeur persistée, pour éviter une désynchronisation si l'utilisateur modifie
  la clé par un autre moyen.

## State Transitions

- **Avertissement de conflit** : détecté au démarrage (si `IgnoreConflictWarning
  == false`) → utilisateur ferme le logiciel concurrent, continue, ou coche
  "ne plus avertir" → `IgnoreConflictWarning` mis à jour et persisté si coché.
- **Lancement au démarrage** : bascule dans les réglages → tentative d'écriture
  de la clé de registre → en cas de succès, `LaunchAtStartup` mis à jour et
  persisté ; en cas d'échec, l'utilisateur est informé et l'état affiché reste
  inchangé (FR-011).
