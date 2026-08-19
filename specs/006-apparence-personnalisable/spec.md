# Feature Specification: Apparence personnalisable

**Feature Branch**: `006-apparence-personnalisable`

**Created**: 2026-08-19

**Status**: Draft

**Input**: User description: « j'aimerais que toutes les interfaces soit
transparente ou qu'on puisse changer la couleur et l'opacité de l'intérieur,
du texte et des bordures ». Précisé ensuite : réglage **global** (3 éléments :
fond, texte, bordures), via une **palette de préréglages + réglage fin**, avec
**aperçu direct sur toutes les fenêtres**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rendre l'interface transparente (Priority: P1) 🎯 MVP

L'utilisateur trouve les fenêtres de Pofus trop présentes par-dessus le jeu.
Il baisse l'opacité du fond pour que le décor de Dofus reste lisible derrière,
et voit le changement immédiatement sur le HUD et le widget.

**Why this priority**: C'est la demande d'origine — la transparence est le
besoin premier, les couleurs viennent ensuite.

**Independent Test**: Ouvrir les réglages, baisser l'opacité du fond, vérifier
que toutes les fenêtres deviennent translucides en direct.

**Acceptance Scenarios**:

1. **Given** les réglages d'apparence sont ouverts, **When** l'utilisateur
   baisse l'opacité du fond, **Then** le HUD et le widget des personnages
   deviennent translucides immédiatement, sans redémarrage.
2. **Given** l'opacité a été modifiée, **When** l'utilisateur relance Pofus,
   **Then** le réglage est conservé.

---

### User Story 2 - Changer les couleurs (Priority: P1) 🎯 MVP

L'utilisateur ajuste la teinte du fond, du texte et des bordures pour
accorder Pofus à son écran ou simplement à son goût.

**Why this priority**: L'autre moitié explicite de la demande ; sans elle on
ne livre qu'un curseur de transparence.

**Independent Test**: Modifier chacune des trois couleurs et vérifier
qu'elles s'appliquent au bon élément sur toutes les fenêtres.

**Acceptance Scenarios**:

1. **Given** les réglages sont ouverts, **When** l'utilisateur change la
   couleur des bordures, **Then** les bordures de toutes les fenêtres Pofus
   changent, sans affecter le fond ni le texte.
2. **Given** l'utilisateur a changé la couleur du texte, **When** il regarde
   n'importe quelle fenêtre de Pofus, **Then** les libellés utilisent la
   nouvelle couleur.

---

### User Story 3 - Partir d'un préréglage (Priority: P2)

L'utilisateur choisit un thème prêt à l'emploi (sombre actuel, verre
translucide, contrasté) puis l'ajuste finement plutôt que de composer chaque
couleur depuis zéro.

**Why this priority**: Confort et découvrabilité ; le réglage fin seul suffit
à couvrir le besoin, donc P2.

**Independent Test**: Appliquer un préréglage, vérifier que les trois
éléments changent d'un coup, puis ajuster un curseur par-dessus.

**Acceptance Scenarios**:

1. **Given** les réglages sont ouverts, **When** l'utilisateur applique le
   préréglage « verre translucide », **Then** fond, texte et bordures
   prennent d'un coup les valeurs de ce préréglage.
2. **Given** un préréglage vient d'être appliqué, **When** l'utilisateur
   modifie ensuite l'opacité du fond, **Then** seule cette valeur change, le
   reste du préréglage est conservé.

---

### Edge Cases

- **Opacité poussée au minimum** : les fenêtres ne DOIVENT jamais devenir
  totalement invisibles ni impossibles à saisir à la souris — un plancher
  d'opacité s'applique, et un bouton « Réinitialiser » restaure les valeurs
  par défaut.
- **Couleur de texte illisible sur le fond choisi** : l'utilisateur reste
  libre de son choix, mais le bouton « Réinitialiser » et la zone de
  notification (qui n'est pas affectée par le thème) offrent toujours une
  porte de sortie.
- **Fenêtres ouvertes au moment du changement** : une fenêtre déjà affichée
  (gestionnaire de comptes, raccourcis…) DOIT adopter la nouvelle apparence
  sans être refermée.
- **Fichier de réglages absent ou corrompu** : l'apparence par défaut
  s'applique, l'incident est journalisé, l'application démarre normalement.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: L'utilisateur DOIT pouvoir régler la **couleur** et
  l'**opacité du fond** des fenêtres de Pofus.
- **FR-002**: L'utilisateur DOIT pouvoir régler la **couleur du texte**.
- **FR-003**: L'utilisateur DOIT pouvoir régler la **couleur** et
  l'**opacité des bordures**.
- **FR-004**: Ces réglages sont **globaux** : ils s'appliquent à toutes les
  fenêtres de Pofus de façon uniforme.
- **FR-005**: Toute modification DOIT être visible **immédiatement** sur
  toutes les fenêtres ouvertes, sans redémarrage ni réouverture.
- **FR-006**: Le système DOIT proposer des **préréglages** applicables en une
  action, ajustables ensuite finement.
- **FR-007**: Les réglages DOIVENT être persistés localement et restaurés au
  démarrage.
- **FR-008**: Un bouton **« Réinitialiser »** DOIT restaurer l'apparence par
  défaut en une action.
- **FR-009**: L'opacité NE DOIT PAS pouvoir descendre sous un plancher qui
  rendrait une fenêtre invisible ou impossible à saisir.
- **FR-010**: Un fichier de réglages absent ou corrompu NE DOIT PAS empêcher
  le démarrage : l'apparence par défaut s'applique et l'incident est
  journalisé explicitement.
- **FR-011**: L'ajout d'une nouvelle fenêtre à Pofus NE DOIT PAS demander de
  code d'apparence spécifique : elle hérite du thème global.

### Key Entities

- **Apparence** : couleur et opacité du fond, couleur du texte, couleur et
  opacité des bordures.
- **Préréglage** : un nom et un jeu complet de valeurs d'apparence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un changement de réglage est visible sur toutes les fenêtres
  ouvertes en moins d'une seconde.
- **SC-002** : L'utilisateur peut rendre Pofus translucide au point de lire
  le jeu derrière, tout en gardant les fenêtres saisissables à la souris.
- **SC-003** : Les réglages survivent à 100% des redémarrages testés.
- **SC-004** : Revenir à l'apparence par défaut demande une seule action.
- **SC-005** : Ajouter une nouvelle fenêtre à Pofus ne demande aucune ligne
  de code liée à l'apparence.

## Assumptions

- Les trois éléments réglables (fond, texte, bordures) suffisent : les
  accents (halo du leader, liseré de focus, couleurs de classe) gardent leur
  rôle fonctionnel et ne sont pas soumis au thème, sans quoi ils cesseraient
  de remplir leur fonction de repère.
- L'écran de réglages d'apparence rejoint la fenêtre « Réglages » existante
  (feature 004) plutôt que d'ouvrir une fenêtre de plus.
- L'icône de la zone de notification n'est pas affectée par le thème : elle
  reste le point d'entrée fiable quel que soit le réglage.
- Le rendu translucide s'appuie sur la transparence déjà activée pour les
  fenêtres de Pofus ; aucun effet système (flou d'arrière-plan) n'est requis
  pour cette version.
