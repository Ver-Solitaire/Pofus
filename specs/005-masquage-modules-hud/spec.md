# Feature Specification: Masquage des fenêtres de Pofus

**Feature Branch**: `005-masquage-modules-hud`

**Created**: 2026-08-19 · **Révisé**: 2026-08-19 (périmètre corrigé)

**Status**: Draft

**Input**: User description: masquer/afficher individuellement chaque fenêtre
de Pofus (le widget listant les personnages, la barre HUD Comptes/Nav/options,
et toute fenêtre future), via un clic droit « Masquer » sur la fenêtre et un
raccourci clavier global dédié et configurable pour la réafficher.

> **Note de révision** — une première version de cette spec portait sur le
> masquage des *boutons de module à l'intérieur* de la barre HUD. Le besoin
> réel est le masquage des *fenêtres elles-mêmes*. Le périmètre ci-dessous
> remplace intégralement le précédent.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Masquer une fenêtre encombrante (Priority: P1) 🎯 MVP

L'utilisateur ne se sert pas en ce moment d'une des fenêtres de Pofus (par
ex. la barre de personnages pendant qu'il joue en mono-compte). Il la masque
d'un clic droit ; elle disparaît de l'écran sans que Pofus s'arrête ni que
les autres fenêtres soient affectées.

**Why this priority**: C'est le cœur de la demande ; sans le masquage, rien
d'autre n'a de sens.

**Independent Test**: Clic droit sur une fenêtre → « Masquer », vérifier
qu'elle disparaît seule.

**Acceptance Scenarios**:

1. **Given** le widget des personnages et la barre HUD sont tous deux
   visibles, **When** l'utilisateur fait un clic droit sur le widget des
   personnages et choisit « Masquer », **Then** ce widget disparaît et la
   barre HUD reste visible et fonctionnelle.
2. **Given** une fenêtre est masquée, **When** l'utilisateur consulte Pofus,
   **Then** l'application continue de tourner normalement (détection des
   comptes, raccourcis de navigation, icône de notification).

---

### User Story 2 - Réafficher une fenêtre par raccourci clavier (Priority: P1) 🎯 MVP

L'utilisateur retrouve une fenêtre masquée par un raccourci clavier global
dédié, sans quitter le jeu.

**Why this priority**: Sans moyen rapide de la faire revenir, le masquage est
un piège à sens unique — indissociable du MVP.

**Independent Test**: Masquer une fenêtre, lui attribuer un raccourci,
appuyer dessus depuis une fenêtre Dofus.

**Acceptance Scenarios**:

1. **Given** le widget des personnages est masqué et un raccourci lui est
   attribué, **When** l'utilisateur appuie sur ce raccourci pendant qu'une
   fenêtre Dofus a le focus, **Then** le widget réapparaît à sa position
   précédente.
2. **Given** une fenêtre est déjà visible, **When** l'utilisateur appuie sur
   son raccourci, **Then** rien ne change et aucune erreur ne survient.
3. **Given** une fenêtre masquée n'a aucun raccourci attribué, **When**
   l'utilisateur ouvre le menu de l'icône de notification, **Then** il peut
   la réafficher depuis ce menu.

---

### User Story 3 - Configurer le raccourci de chaque fenêtre (Priority: P2)

L'utilisateur choisit la combinaison associée à chaque fenêtre, avec
détection des conflits, comme pour les raccourcis de navigation.

**Why this priority**: Attendu par cohérence, mais le masquage reste
utilisable sans (le menu de notification sert de repli).

**Independent Test**: Attribuer une combinaison déjà prise par une autre
fenêtre ou par une action de navigation, vérifier le refus explicite.

**Acceptance Scenarios**:

1. **Given** l'écran des raccourcis est ouvert, **When** l'utilisateur
   assigne à une fenêtre une combinaison déjà utilisée par l'action
   « Compte suivant », **Then** le conflit est signalé et la liaison n'est
   pas appliquée.
2. **Given** un raccourci vient d'être attribué, **When** l'utilisateur
   relance Pofus, **Then** le raccourci est toujours actif.

---

### Edge Cases

- Toutes les fenêtres masquées : Pofus continue de tourner ; l'icône de
  notification reste le point d'entrée garanti pour tout réafficher.
- Raccourci impossible à enregistrer (déjà réservé par un autre logiciel) :
  échec journalisé explicitement, la fenêtre reste réaffichable depuis le
  menu de notification, le reste de l'application n'est pas affecté.
- Réafficher une fenêtre déjà visible : sans effet (idempotent).
- Une fenêtre masquée conserve sa position à l'écran ; elle réapparaît là où
  elle était, pas à une position par défaut.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Chaque fenêtre de Pofus déclarée masquable DOIT pouvoir être
  masquée individuellement par un clic droit sur la fenêtre suivi du choix
  « Masquer ».
- **FR-002**: Masquer une fenêtre DOIT la faire disparaître immédiatement
  sans fermer l'application ni affecter les autres fenêtres.
- **FR-003**: Masquer une fenêtre ne DOIT pas interrompre le fonctionnement
  de Pofus en arrière-plan (détection des comptes, raccourcis globaux).
- **FR-004**: Chaque fenêtre masquable DOIT pouvoir recevoir un raccourci
  clavier global dédié qui la réaffiche.
- **FR-005**: Ce raccourci DOIT fonctionner quelle que soit la fenêtre ayant
  le focus, y compris une fenêtre Dofus.
- **FR-006**: Tout conflit entre le raccourci d'une fenêtre et un autre
  raccourci déjà attribué (autre fenêtre ou action de navigation) DOIT être
  signalé explicitement, sans remplacement silencieux.
- **FR-007**: L'état masqué/affiché de chaque fenêtre DOIT être persisté
  localement et restauré au prochain démarrage.
- **FR-008**: Les liaisons de raccourci DOIVENT être persistées et
  applicables à chaud, sans redémarrage.
- **FR-009**: Le menu de l'icône de notification DOIT permettre de réafficher
  n'importe quelle fenêtre masquée, y compris sans raccourci configuré —
  point d'entrée garanti quel que soit l'état des fenêtres.
- **FR-010**: Une fenêtre réaffichée DOIT retrouver la position qu'elle avait
  avant d'être masquée.
- **FR-011**: Tout échec d'enregistrement d'un raccourci DOIT être journalisé
  explicitement sans empêcher le reste de l'application de fonctionner.
- **FR-012**: Le mécanisme DOIT être générique : déclarer une nouvelle
  fenêtre masquable ne DOIT pas demander de recoder le système de masquage,
  de persistance ou de raccourcis.

### Key Entities

- **Fenêtre masquable** : identifiant stable, libellé affiché, état
  masqué/affiché, raccourci de réaffichage optionnel.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Masquer une fenêtre demande une seule action directe (clic
  droit → une entrée de menu).
- **SC-002** : Une fenêtre masquée réapparaît en moins d'une seconde après
  l'appui sur son raccourci.
- **SC-003** : L'état masqué/affiché survit à 100% des redémarrages testés.
- **SC-004** : 100% des conflits de raccourci sont signalés avant toute
  application.
- **SC-005** : Ajouter une nouvelle fenêtre masquable ne demande que sa
  déclaration (identifiant + libellé + instance), sans modification du
  moteur de masquage.

## Assumptions

- Les deux fenêtres masquables à ce jour sont le widget des personnages et la
  barre HUD ; le mécanisme est générique pour les fenêtres futures (FR-012).
- Masquer une fenêtre la cache sans la fermer : son état interne (position,
  contenu, minuteries) est conservé, ce qui satisfait FR-010 naturellement.
- Aucun raccourci par défaut n'est pré-assigné, pour éviter tout conflit
  silencieux avec les raccourcis de navigation qui, eux, ont des valeurs par
  défaut.
- L'écran de configuration des raccourcis existant (feature 003) est étendu
  plutôt que dupliqué.
