# Research: Paramètres — Détection de Conflits et Démarrage

## Decision: Détection de processus via `System.Diagnostics.Process`

**Rationale**: `Process.GetProcessesByName` (BCL standard, aucun P/Invoke
nécessaire) suffit pour vérifier si un exécutable connu tourne déjà — reprend
le principe du projet de référence (`tasklist_has`, qui interroge `tasklist`
en sous-processus) mais via l'API .NET native, plus directe et sans
dépendance à un outil externe. La fermeture réutilise `Process.Kill()`,
équivalent managé du `taskkill` du projet de référence.

**Alternatives considered**:
- **Appel à `tasklist`/`taskkill` en sous-processus (comme le projet de
  référence)** : rejeté — `Process.GetProcessesByName`/`Process.Kill()`
  offrent la même capacité sans dépendre d'un processus externe ni parser de
  sortie texte.

## Decision: Lancement au démarrage via la clé de registre `Run`

**Rationale**: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
est le mécanisme standard, par utilisateur (pas besoin de droits
administrateur), pour démarrer une application à l'ouverture de session
Windows. `Microsoft.Win32.Registry` (BCL) permet de lire/écrire cette clé
sans P/Invoke.

**Alternatives considered**:
- **Tâche planifiée Windows (Task Scheduler)** : rejeté — plus de
  fonctionnalités que nécessaire (déclencheurs multiples, permissions),
  complexité non justifiée pour un simple lancement à la connexion.
- **Raccourci dans le dossier Démarrage** : rejeté — la clé de registre est
  plus simple à activer/désactiver par le code et ne laisse pas de fichier
  orphelin si Pofus est déplacé ou désinstallé sans nettoyage.

## Decision: Abstraction pour la testabilité

**Rationale**: `Process.GetProcessesByName`/`Process.Kill()` et
`Microsoft.Win32.Registry` sont des appels statiques/système difficiles à
simuler directement. Comme pour l'interop Win32 des fonctionnalités
précédentes, ils sont encapsulés derrière des interfaces
(`IProcessController`, `IStartupRegistration`) dans `Pofus.Platform`, avec une
implémentation réelle et un double de test — cohérent avec le pattern déjà
établi (`IWin32WindowApi`).

**Alternatives considered**: Aucune — cohérent avec le pattern déjà en place.

## Decision: Liste de logiciels concurrents extensible

**Rationale**: Un seul nom connu pour cette version (`organizer`, repris du
projet de référence), mais stocké comme liste (`Pofus.Core`, donnée pure)
plutôt qu'une valeur unique, pour permettre d'en ajouter d'autres plus tard
sans changer la logique de détection.

**Alternatives considered**: Aucune.

## Outstanding NEEDS CLARIFICATION

Aucune.
