# Research: Gestion des Comptes Dofus

## Decision: Filtrage "compte réel" vs fenêtre de lancement

**Rationale**: Le projet de référence (Doframe, `logic.py::scan_slots`) exclut
toute fenêtre dont le titre commence par "dofus" (insensible à la casse) ou est
vide — ce sont les fenêtres de lancement/sélection de personnage, pas des
comptes en jeu. Il découpe ensuite le titre sur `" - "` : `parts[0]` = pseudo,
`parts[1]` = classe (sinon "Inconnu"). Ce format a été confirmé sur un client
réel pendant les tests de la feature 001 : `"Mon-Perso - Ouginak - 3.6.10.10 -
Release"` → pseudo `"Mon-Perso"`, classe `"Ouginak"`.

Cette logique est réimplémentée telle quelle dans un `AccountTitleParser` de
`Pofus.Core` (pure, sans dépendance Win32), consommant la liste brute déjà
fournie par `IDofusWindowLocator` (feature 001) — pas besoin de filtrer par
classe de fenêtre Win32 (`UnityWndClass` chez Doframe) tant que le filtrage par
nom de processus + exclusion de titre suffit à écarter les faux positifs.

**Alternatives considered**:
- **Filtrer aussi par classe de fenêtre Win32** : rejeté pour cette version —
  ajoute une dépendance Platform supplémentaire pour un gain marginal, le
  filtrage par process + titre déjà en place s'est avéré suffisant en test réel.

## Decision: Rafraîchissement par sondage périodique (polling)

**Rationale**: Contrairement à la réassertion always-on-top (feature 001,
`EVENT_SYSTEM_FOREGROUND`), il n'existe pas d'évènement Win32 générique
"une nouvelle fenêtre Dofus vient d'apparaître" auquel s'abonner. Un sondage
périodique (`DispatcherTimer`, intervalle 2s) appelant `GetOpenDofusWindows()`
est donc la seule option pragmatique, cohérente avec SC-001 (détection en moins
de 5 secondes). Le coût CPU reste négligeable : l'énumération de fenêtres est
rapide et l'intervalle n'est pas agressif.

**Alternatives considered**:
- **`SetWinEventHook(EVENT_OBJECT_CREATE)` global** : rejeté — bien plus
  bruyant (déclenché par toute création de fenêtre sur le système, pas
  seulement Dofus), complexité non justifiée par le gain face à un polling à
  2s.

## Decision: Persistance des préférences de comptes

**Rationale**: Fichier JSON local dédié
(`%APPDATA%\Pofus\account-preferences.json`), séparé de `hud-layout.json`
(feature 001) car ce sont deux domaines indépendants (disposition du HUD vs
préférences par compte). Structure : état actif/inactif par pseudo, équipe par
pseudo, pseudo du leader, ordre personnalisé (liste de pseudos, plafonnée à 50
entrées avec purge des plus anciennes non détectées — reprend la limite déjà
en place chez Doframe).

**Alternatives considered**:
- **Un seul fichier de préférences pour tout Pofus** : rejeté pour l'instant —
  chaque module gérant son propre fichier de persistance reste plus simple à
  faire évoluer indépendamment (cohérent avec Principe III).

## Decision: Le slot HUD "accounts" héberge un module réel via `IHudModule`

**Rationale**: La feature 001 a livré le contrat `IHudModule` et son hôte
(`ModuleHost`) sans implémentation concrète. Cette fonctionnalité est la
première à l'implémenter : `AccountsHudModule` expose un contenu compact
(nombre de comptes actifs + initiale du leader) dans le slot, et ouvre une
fenêtre dédiée (`AccountManagerWindow`) au clic — reprenant le "gestionnaire de
personnages dédié" du projet de référence plutôt que de faire tenir la liste
complète dans un slot de 40×40 px.

**Alternatives considered**:
- **Afficher la liste complète directement dans le slot** : rejeté, un slot de
  40×40 px ne peut pas accueillir une liste de 8+ comptes lisiblement.

## Decision: Réordonnancement via boutons Monter/Descendre plutôt que glisser-déposer

**Rationale**: FR-009 exige que l'utilisateur puisse réordonner manuellement
les comptes actifs. Un vrai glisser-déposer dans une `ListBox` WPF est
réalisable mais ajoute une complexité et une surface de bugs (feedback visuel
pendant le drag, calcul d'index de dépôt, réordonnancement multi-sélection)
disproportionnée pour cette version. Des boutons "Monter"/"Descendre" par ligne
satisfont la même exigence fonctionnelle avec une implémentation beaucoup plus
simple et robuste (Principe I — robustesse avant tout).

**Alternatives considered**:
- **Glisser-déposer complet** : reporté à une itération future si le besoin
  utilisateur se confirme.

## Outstanding NEEDS CLARIFICATION

Aucune.
