# Feature Specification: Navigation Rapide Entre Fenêtres Dofus

**Feature Branch**: `003-navigation-fenetres-dofus`

**Created**: 2026-08-18

**Status**: Draft

**Input**: User description: "Module de navigation rapide entre les fenêtres Dofus via raccourcis clavier configurables : basculer vers le compte suivant, le compte précédent, et aller directement au compte leader (désigné dans le module Gestion des comptes), le tout déclenché par des raccourcis clavier que l'utilisateur peut configurer. Reprend le comportement de navigation (next_char/prev_char/focus_leader) du projet de référence Doframe, mais sans les macros d'automatisation (clic synchronisé, auto-zaap, invitation de groupe, etc.) qui restent hors périmètre pour l'instant."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basculer entre mes comptes au clavier (Priority: P1)

En tant qu'utilisateur multi-comptes, je veux passer d'un personnage à l'autre
via un raccourci clavier, sans utiliser la souris ni chercher la bonne fenêtre
dans la barre des tâches, afin de garder les mains sur le clavier pendant le
jeu.

**Why this priority**: C'est la valeur centrale du module — sans elle, rien
d'autre n'a de sens.

**Independent Test**: Avec plusieurs comptes actifs détectés (cf. feature 002),
déclencher le raccourci "compte suivant" et vérifier que la fenêtre Dofus du
personnage suivant passe au premier plan ; répéter avec "compte précédent".

**Acceptance Scenarios**:

1. **Given** plusieurs comptes actifs sont détectés, **When** l'utilisateur
   déclenche le raccourci "compte suivant", **Then** la fenêtre Dofus du
   personnage suivant dans la liste passe au premier plan.
2. **Given** l'utilisateur est sur le dernier compte actif de la liste,
   **When** il déclenche "compte suivant", **Then** la navigation reprend au
   premier compte actif (boucle).
3. **Given** un seul compte actif est détecté, **When** l'utilisateur
   déclenche "compte suivant" ou "compte précédent", **Then** rien ne change
   visuellement et aucune erreur ne se produit.
4. **Given** aucun compte actif n'est détecté, **When** l'utilisateur
   déclenche un raccourci de navigation, **Then** rien ne se produit, sans
   erreur ni plantage.
5. **Given** l'utilisateur change manuellement de fenêtre Dofus (clic, Alt-Tab)
   plutôt que d'utiliser un raccourci, **When** il déclenche ensuite "compte
   suivant", **Then** la navigation reprend à partir du compte réellement
   affiché au premier plan, pas de la dernière position connue du raccourci.

---

### User Story 2 - Rejoindre le leader instantanément (Priority: P2)

En tant qu'utilisateur ayant désigné un leader (cf. feature 002), je veux un
raccourci dédié pour basculer directement vers sa fenêtre, sans avoir à le
chercher dans le cycle suivant/précédent.

**Why this priority**: Complète la navigation de base mais n'est utile que si
un leader a été désigné — dépend d'US1 pour le mécanisme de focus de fenêtre.

**Independent Test**: Désigner un leader (feature 002), déclencher le
raccourci "aller au leader" depuis n'importe quel autre compte, vérifier que
sa fenêtre passe au premier plan.

**Acceptance Scenarios**:

1. **Given** un leader est désigné et détecté, **When** l'utilisateur
   déclenche le raccourci "aller au leader", **Then** la fenêtre Dofus du
   leader passe au premier plan, quelle que soit la fenêtre active avant.
2. **Given** aucun leader n'est désigné, **When** l'utilisateur déclenche ce
   raccourci, **Then** rien ne se produit, sans erreur.
3. **Given** un leader est désigné mais son compte n'est pas actuellement
   détecté (déconnecté), **When** l'utilisateur déclenche ce raccourci,
   **Then** rien ne se produit, sans erreur.

---

### User Story 3 - Choisir mes propres raccourcis (Priority: P3)

En tant qu'utilisateur, je veux pouvoir changer la combinaison de touches
assignée à chaque action de navigation, afin d'éviter les conflits avec
d'autres logiciels ou mes propres habitudes.

**Why this priority**: Les raccourcis par défaut couvrent déjà le besoin de
base (US1/US2) ; la personnalisation est un confort additionnel.

**Independent Test**: Changer la combinaison assignée à "compte suivant",
vérifier qu'elle est immédiatement active, puis redémarrer Pofus et vérifier
qu'elle est conservée.

**Acceptance Scenarios**:

1. **Given** l'utilisateur modifie la combinaison assignée à une action de
   navigation, **When** il déclenche la nouvelle combinaison, **Then** l'action
   correspondante s'exécute, sans redémarrage de Pofus requis.
2. **Given** une combinaison est déjà assignée à une action de navigation,
   **When** l'utilisateur tente d'assigner la même combinaison à une autre
   action de navigation, **Then** le système refuse et indique le conflit
   plutôt que d'assigner silencieusement les deux actions à la même touche.
3. **Given** des raccourcis personnalisés ont été configurés, **When** Pofus
   redémarre, **Then** ils sont conservés à l'identique.

---

### Edge Cases

- Que se passe-t-il si l'utilisateur désactive (feature 002) le compte
  actuellement affiché pendant qu'il navigue ? Le compte désactivé doit
  disparaître du cycle suivant/précédent dès la prochaine navigation.
- Que se passe-t-il si un raccourci de navigation est déclenché alors qu'une
  fenêtre Dofus a le focus (cas normal d'usage, pas seulement quand Pofus a le
  focus) ?
- Que se passe-t-il si l'utilisateur essaie d'assigner une combinaison vide ou
  invalide à une action ? Le système doit refuser plutôt que désactiver
  silencieusement le raccourci.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre de basculer vers la fenêtre du compte
  actif suivant via un raccourci clavier dédié.
- **FR-002**: Le système DOIT permettre de basculer vers la fenêtre du compte
  actif précédent via un raccourci clavier dédié.
- **FR-003**: Le système DOIT permettre de basculer directement vers la
  fenêtre du compte leader désigné via un raccourci clavier dédié.
- **FR-004**: La navigation suivant/précédent DOIT uniquement parcourir les
  comptes actuellement actifs (comptes désactivés exclus, cf. feature 002),
  dans l'ordre personnalisé existant.
- **FR-005**: L'utilisateur DOIT pouvoir configurer la combinaison de touches
  assignée à chacune des trois actions de navigation.
- **FR-006**: Le système DOIT refuser d'assigner une combinaison de touches
  déjà utilisée par une autre action de navigation, et l'indiquer clairement
  à l'utilisateur.
- **FR-007**: Les combinaisons de touches configurées DOIVENT être conservées
  entre les sessions.
- **FR-008**: Déclencher un raccourci de navigation alors qu'aucun compte
  actif (ou aucun leader, pour ce raccourci précis) n'est détecté NE DOIT
  produire aucune erreur ni action visible.
- **FR-009**: Les raccourcis DOIVENT fonctionner quelle que soit la fenêtre
  actuellement au premier plan, y compris une fenêtre de jeu Dofus.
- **FR-010**: La position courante dans le cycle de navigation DOIT rester
  cohérente avec la fenêtre Dofus réellement au premier plan, même après un
  changement de fenêtre effectué manuellement par l'utilisateur (hors
  raccourcis de ce module).

### Key Entities

- **NavigationShortcut**: Une action de navigation (Suivant, Précédent, Aller
  au leader) et la combinaison de touches qui la déclenche.
- **NavigationShortcutPreferences**: L'ensemble des combinaisons configurées
  par l'utilisateur, persistant entre les sessions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Depuis n'importe quelle fenêtre Dofus active, basculer vers le
  compte suivant ou précédent prend effet en moins d'une seconde perçue.
- **SC-002**: Un utilisateur reconfigure un raccourci et le voit fonctionner
  immédiatement, sans redémarrer Pofus.
- **SC-003**: Les raccourcis configurés sont restaurés à l'identique après
  redémarrage de Pofus dans 100 % des cas.
- **SC-004**: Avec 8 comptes actifs, un utilisateur parcourt l'ensemble des
  comptes en utilisant uniquement le clavier, sans manipulation de souris.

## Assumptions

- Seuls les comptes marqués actifs (feature 002 — Gestion des Comptes)
  participent au cycle suivant/précédent ; les comptes désactivés ou non
  détectés en sont exclus.
- Les combinaisons par défaut évitent les touches seules (contrairement au
  projet de référence, qui utilisait par exemple "tab" ou "²" en touche
  unique) pour ne pas interférer avec la saisie de texte normale ou les
  contrôles du jeu — des combinaisons avec modificateur (Ctrl/Alt/Maj) sont
  utilisées par défaut.
- La détection de conflit (FR-006) ne couvre que les trois raccourcis de ce
  module entre eux ; les conflits avec des raccourcis d'autres logiciels ou du
  système d'exploitation sont hors périmètre.
- Les macros d'automatisation (clic synchronisé, auto-zaap, invitation de
  groupe, échange de drop XP, coller+entrée...) restent explicitement hors
  périmètre de cette fonctionnalité — seule la navigation entre fenêtres est
  couverte.
- Cette fonctionnalité dépend de la feature 002 (Gestion des Comptes) pour la
  liste des comptes actifs et la désignation du leader.
