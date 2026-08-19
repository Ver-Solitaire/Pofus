# Feature Specification: Paramètres — Détection de Conflits et Démarrage

**Feature Branch**: `004-parametres-conflits-demarrage`

**Created**: 2026-08-18

**Status**: Draft

**Input**: User description: "Détection de logiciels concurrents connus (ex. organizer.exe, comme le fait le projet de référence Doframe) au démarrage de Pofus, avec avertissement à l'utilisateur et possibilité de fermer le logiciel concurrent ou d'ignorer l'avertissement définitivement. Plus une fenêtre de réglages généraux accessible depuis le slot 'settings' du HUD, avec au minimum le lancement automatique de Pofus au démarrage de Windows et la réinitialisation de l'avertissement de conflit. La vitesse de clic et le choix de disposition clavier AZERTY/QWERTY restent hors périmètre pour cette version."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Être alerté d'un logiciel concurrent au démarrage (Priority: P1)

En tant qu'utilisateur, je veux être prévenu si un autre logiciel de gestion
multi-comptes connu pour entrer en conflit tourne déjà, afin d'éviter des
comportements imprévisibles avant même de commencer à utiliser Pofus.

**Why this priority**: C'est la valeur centrale du module — repérer un conflit
tôt évite des bugs difficiles à diagnostiquer plus tard dans la session.

**Independent Test**: Lancer un exécutable nommé comme un logiciel concurrent
connu, puis démarrer Pofus : vérifier qu'un avertissement apparaît.

**Acceptance Scenarios**:

1. **Given** un logiciel concurrent connu est en cours d'exécution, **When**
   l'utilisateur démarre Pofus, **Then** un avertissement explicite apparaît,
   nommant le logiciel détecté.
2. **Given** l'avertissement est affiché, **When** l'utilisateur choisit de
   fermer le logiciel concurrent, **Then** celui-ci se ferme et l'avertissement
   se referme.
3. **Given** l'avertissement est affiché, **When** l'utilisateur choisit de
   continuer sans le fermer, **Then** l'avertissement se referme et Pofus
   continue de fonctionner normalement.
4. **Given** aucun logiciel concurrent connu n'est en cours d'exécution,
   **When** l'utilisateur démarre Pofus, **Then** aucun avertissement
   n'apparaît et le démarrage n'est pas interrompu.

---

### User Story 2 - Lancer Pofus automatiquement avec Windows (Priority: P2)

En tant qu'utilisateur, je veux que Pofus démarre automatiquement à l'ouverture
de session Windows, afin de ne pas avoir à le relancer manuellement à chaque
fois que je joue.

**Why this priority**: Confort d'usage important pour un usage régulier, mais
non bloquant pour la valeur de base du module (US1).

**Independent Test**: Activer le lancement automatique dans les réglages,
redémarrer la session Windows, vérifier que Pofus démarre seul.

**Acceptance Scenarios**:

1. **Given** l'utilisateur ouvre la fenêtre de réglages généraux, **When** il
   active le lancement automatique au démarrage de Windows, **Then** ce choix
   est appliqué et conservé.
2. **Given** le lancement automatique est activé, **When** l'utilisateur
   rouvre la session Windows, **Then** Pofus démarre sans action manuelle.
3. **Given** le lancement automatique est activé, **When** l'utilisateur le
   désactive depuis les réglages, **Then** Pofus ne démarre plus
   automatiquement à la session suivante.

---

### User Story 3 - Revenir sur le choix d'ignorer l'avertissement (Priority: P3)

En tant qu'utilisateur ayant choisi d'ignorer définitivement l'avertissement de
conflit, je veux pouvoir revenir sur ce choix depuis les réglages, au cas où je
changerais d'avis plus tard.

**Why this priority**: Cas de correction d'un choix précédent — utile mais
secondaire, l'utilisateur peut aussi simplement composer avec le logiciel
concurrent en attendant.

**Independent Test**: Ignorer l'avertissement définitivement, puis le
réinitialiser depuis les réglages, relancer Pofus avec le logiciel concurrent
actif : vérifier que l'avertissement réapparaît.

**Acceptance Scenarios**:

1. **Given** l'utilisateur a précédemment choisi d'ignorer l'avertissement de
   conflit, **When** il ouvre les réglages généraux, **Then** une option pour
   réinitialiser ce choix est visible.
2. **Given** l'utilisateur réinitialise ce choix, **When** Pofus redémarre
   avec un logiciel concurrent actif, **Then** l'avertissement réapparaît.

---

### Edge Cases

- Que se passe-t-il si la fermeture du logiciel concurrent échoue (droits
  insuffisants, processus protégé) ? L'utilisateur doit être informé de
  l'échec plutôt que de croire l'opération réussie silencieusement.
- Que se passe-t-il si le logiciel concurrent est relancé après avoir été
  fermé, alors que l'avertissement a été ignoré définitivement ? Aucun
  nouvel avertissement n'apparaît (cohérent avec le choix persistant de
  l'utilisateur).
- Que se passe-t-il si l'activation du lancement automatique échoue (ex.
  droits insuffisants) ? L'utilisateur doit être informé plutôt que de croire
  le réglage appliqué silencieusement.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT vérifier, au démarrage de Pofus, si un logiciel
  concurrent connu est en cours d'exécution.
- **FR-002**: Si un logiciel concurrent est détecté et que l'utilisateur n'a
  pas choisi d'ignorer cet avertissement, le système DOIT afficher un
  avertissement explicite nommant le logiciel détecté.
- **FR-003**: Depuis cet avertissement, l'utilisateur DOIT pouvoir fermer le
  logiciel concurrent directement.
- **FR-004**: Depuis cet avertissement, l'utilisateur DOIT pouvoir continuer
  sans fermer le logiciel concurrent.
- **FR-005**: L'utilisateur DOIT pouvoir choisir, depuis l'avertissement, de ne
  plus être averti de ce conflit à l'avenir.
- **FR-006**: Ce choix DOIT persister entre les sessions jusqu'à
  réinitialisation explicite (US3).
- **FR-007**: L'utilisateur DOIT pouvoir ouvrir une fenêtre de réglages
  généraux depuis le HUD.
- **FR-008**: Depuis cette fenêtre, l'utilisateur DOIT pouvoir activer ou
  désactiver le lancement automatique de Pofus au démarrage de Windows.
- **FR-009**: Depuis cette fenêtre, l'utilisateur DOIT pouvoir réinitialiser
  son choix d'ignorer l'avertissement de conflit.
- **FR-010**: Si aucun logiciel concurrent connu n'est détecté, le système NE
  DOIT PAS afficher d'avertissement ni interrompre le démarrage.
- **FR-011**: Un échec lors de la fermeture du logiciel concurrent ou de
  l'activation du lancement automatique NE DOIT PAS faire planter Pofus, et
  DOIT être signalé explicitement à l'utilisateur.

### Key Entities

- **ConflictWarningPreference**: Le choix persistant de l'utilisateur
  d'ignorer ou non l'avertissement de conflit logiciel.
- **StartupLaunchPreference**: L'état activé/désactivé du lancement
  automatique de Pofus au démarrage de Windows.
- **KnownConflictingSoftware**: La liste des logiciels concurrents connus
  recherchés au démarrage (initialement limitée à un seul, reprenant le
  projet de référence, mais pensée pour être étendue).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un logiciel concurrent déjà en cours d'exécution est détecté et
  signalé dans les 5 secondes suivant le lancement de Pofus.
- **SC-002**: Un utilisateur ferme le logiciel concurrent et reprend son
  activité en une seule action depuis l'avertissement.
- **SC-003**: Un utilisateur qui active le lancement automatique voit Pofus
  démarrer avec Windows au redémarrage suivant, sans action supplémentaire.
- **SC-004**: Une fois l'avertissement ignoré définitivement, il n'est plus
  jamais reproposé tant que l'utilisateur ne le réinitialise pas
  explicitement depuis les réglages.

## Assumptions

- Le seul logiciel concurrent connu pour cette version est celui déjà
  identifié par le projet de référence (organizer.exe) ; la liste est conçue
  pour être étendue facilement par la suite sans redesign.
- La détection de conflit s'exécute une fois au démarrage de Pofus, pas en
  continu pendant la session (cohérent avec le comportement du projet de
  référence).
- La vitesse de clic et le choix explicite de disposition clavier
  AZERTY/QWERTY restent hors périmètre : la première n'a pas d'ancrage tant
  qu'aucune macro de clic n'existe, et la seconde est déjà résolue par la
  conception des raccourcis clavier existants (indépendants de la disposition
  physique du clavier).
- "Réglages généraux" pour cette version ne couvre que le lancement au
  démarrage de Windows et la réinitialisation de l'avertissement de conflit ;
  d'autres réglages pourront s'y ajouter dans de futures fonctionnalités.
