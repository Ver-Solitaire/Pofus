<!--
Sync Impact Report
- Version change: [TEMPLATE] → 1.0.0 (initial ratification)
- Modified principles: n/a (first ratification, all principles newly defined)
- Added sections: Core Principles (I–V), Contraintes Techniques, Workflow de Développement, Governance
- Removed sections: none
- Templates requiring follow-up: none — plan/spec/tasks templates already reference
  the constitution generically and need no structural change.
- Deferred TODOs: TECH_STACK is intentionally left open; finalize during /speckit-plan
  for the first feature rather than locking it here.
-->

# Pofus Constitution

## Core Principles

### I. Robustesse avant tout (NON-NÉGOCIABLE)
Chaque chemin d'erreur DOIT être géré explicitement et journalisé ; les blocs
`except` silencieux (`except: pass`) sont interdits. Toute intégration système
fragile (fenêtres externes, périphériques, réseau) DOIT échouer proprement,
sans crash de l'application ni état incohérent laissé derrière elle. La
qualité de production prime sur la vitesse de livraison : une fonctionnalité
incomplète ou mal gérée en cas d'erreur n'est pas mergeable.

### II. Fluidité et performance mesurées
Le logiciel DOIT rester réactif : aucune opération bloquante (E/S, appels
Win32, réseau) ne s'exécute sur le thread d'interface. Les automatisations
(clics, macros, bascules de personnages) DOIVENT viser une latence perçue
minimale. Toute optimisation DOIT être justifiée par une amélioration
mesurable (latence, usage CPU/mémoire) et non ajoutée par anticipation.

### III. Séparation stricte des responsabilités
L'architecture DOIT séparer clairement : configuration/persistance, logique
métier, interface utilisateur, et intégration système (Win32, clavier,
systray). Chaque module DOIT être testable indépendamment de l'interface
graphique. Pas de logique métier dans le code d'UI.

### IV. Intégration Windows fiable
Les interactions bas niveau (API Win32, dispositions clavier, gestion multi-
fenêtres, DPI) DOIVENT être encapsulées dans des modules dédiés avec
détection explicite des cas d'échec (fenêtre introuvable, API indisponible,
conflit avec un autre logiciel). Aucune supposition silencieuse sur l'état du
système externe.

### V. Usage personnel et respect des données locales
Le logiciel reste un outil d'usage personnel, exécuté localement, sans
transmission de données (comptes, identifiants, configuration) vers un tiers
sans action explicite de l'utilisateur. Toute fonctionnalité réseau
(vérification de version, télémétrie) DOIT être opt-in et clairement
documentée.

## Contraintes Techniques

- Plateforme cible : Windows 10/11 exclusivement (dépendance à l'API Win32).
- Stack technique définitive non figée ici : elle est choisie et justifiée
  lors du `/speckit-plan` de la première fonctionnalité, en cohérence avec
  les principes ci-dessus (réactivité, séparation des responsabilités).
- Pofus est une réécriture complète inspirée fonctionnellement d'un projet
  existant (gestion multi-comptes, macros, menu radial, overlay, systray) :
  le code source de référence sert de spécification comportementale, pas de
  base à copier telle quelle — chaque module est reconçu pour la robustesse
  et la performance.

## Workflow de Développement

- Toute nouvelle fonctionnalité suit le cycle spec-kit : `/speckit-specify` →
  `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`.
- `/speckit-analyze` est recommandé avant `/speckit-implement` dès qu'une
  fonctionnalité touche l'intégration système (Win32, macros, hotkeys), zone
  la plus à risque de régressions silencieuses.
- Les tâches touchant la Principe I (gestion d'erreurs) DOIVENT inclure un
  scénario d'échec explicite dans leurs critères d'acceptation.

## Governance

La constitution prévaut sur toute autre pratique ou préférence ponctuelle.
Toute modification passe par `/speckit-constitution`, avec mise à jour du
numéro de version selon le versionnage sémantique (MAJOR : suppression ou
redéfinition incompatible d'un principe ; MINOR : ajout de principe ou
extension notable ; PATCH : clarification sans changement de sens). Les
revues de plan (`/speckit-plan`, `/speckit-analyze`) DOIVENT vérifier la
conformité aux principes ci-dessus ; toute dérogation DOIT être justifiée
explicitement dans le plan concerné.

**Version**: 1.0.0 | **Ratified**: 2026-08-18 | **Last Amended**: 2026-08-18
