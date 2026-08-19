# Feature Specification: HUD Modulaire Style Dofus

**Feature Branch**: `001-hud-modulaire-dofus`

**Created**: 2026-08-18

**Status**: Draft

**Input**: User description: "Un HUD dans un style épuré dans lequel je pourrais placer chacun des 'modules' de mon outil, en reprenant visuellement l'esthétique de Dofus (captures d'écran fournies : barres sombres à coins arrondis, bordures dorées/bronze, icônes circulaires d'action, orbe de vie, icônes de ressources, compteur de tour hexagonal, panneau de chat/notifications)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Afficher un HUD unique par-dessus toutes les fenêtres Dofus (Priority: P1)

En tant qu'utilisateur multi-comptes, je veux un seul et même HUD, toujours visible
au-dessus de l'ensemble des fenêtres Dofus ouvertes (jusqu'à une dizaine de comptes
simultanés), afin de piloter tous mes comptes depuis un point central sans avoir un
HUD différent par fenêtre ni devoir faire passer une fenêtre au premier plan pour
retrouver mes outils.

**Why this priority**: Sans un HUD unique et stable au-dessus de toutes les fenêtres,
aucun module ne peut piloter plusieurs comptes à la fois en pratique — c'est le socle
de toute la fonctionnalité.

**Independent Test**: Lancer plusieurs fenêtres Dofus (plusieurs comptes), afficher le
HUD, vérifier qu'un seul HUD apparaît et reste visible au-dessus de chacune des
fenêtres, quelle que soit celle actuellement au premier plan.

**Acceptance Scenarios**:

1. **Given** plusieurs fenêtres Dofus sont ouvertes (plusieurs comptes) et le HUD est
   activé, **When** l'utilisateur fait passer une fenêtre Dofus différente au premier
   plan (clic sur une autre fenêtre, alt-tab), **Then** le même HUD reste visible
   au-dessus de la fenêtre nouvellement active, sans se dupliquer ni disparaître.
2. **Given** le HUD est affiché, **When** l'utilisateur déplace ou redimensionne une
   fenêtre de jeu, **Then** le HUD garde sa propre position à l'écran (définie par
   l'utilisateur, voir US2) et reste au-dessus de toutes les fenêtres Dofus concernées.
3. **Given** le HUD est affiché, **When** l'utilisateur demande à le masquer, **Then**
   le HUD disparaît immédiatement — pour toutes les fenêtres à la fois — sans fermer
   l'outil ni interrompre ses automatisations en cours.

---

### User Story 2 - Organiser les modules dans le HUD (Priority: P2)

En tant qu'utilisateur, je veux que le HUD présente des emplacements dédiés pour
chacun des modules de mon outil (gestion de comptes, macros, menu radial, etc.) afin
de retrouver chaque fonctionnalité à un endroit prévisible.

**Why this priority**: Le HUD n'a de valeur que s'il sait accueillir plusieurs modules
de façon organisée ; c'est ce qui le distingue d'une simple fenêtre unique.

**Independent Test**: Avec le HUD affiché, vérifier que chaque emplacement de module
est visuellement distinct, identifiable, et conserve sa position d'une session à
l'autre.

**Acceptance Scenarios**:

1. **Given** le HUD est affiché avec plusieurs emplacements de modules, **When**
   l'utilisateur observe le HUD, **Then** chaque emplacement est visuellement délimité
   et associé à une icône ou un libellé identifiant le module qu'il contient.
2. **Given** l'utilisateur a disposé ses modules dans le HUD, **When** il redémarre
   l'outil, **Then** la disposition des modules est conservée telle qu'il l'avait
   laissée.

---

### User Story 3 - Bénéficier d'une esthétique cohérente avec Dofus (Priority: P3)

En tant qu'utilisateur, je veux que le HUD reprenne les codes visuels de l'interface
de Dofus (panneaux sombres à coins arrondis, bordures dorées/bronze, icônes rondes,
compteurs hexagonaux) afin qu'il s'intègre naturellement à l'écran de jeu plutôt que
de ressembler à une fenêtre Windows générique.

**Why this priority**: L'intégration visuelle améliore le confort d'usage mais
n'est pas bloquante pour la fonction première du HUD (afficher et organiser des
modules) — elle peut être affinée après la mise en place du socle fonctionnel.

**Independent Test**: Comparer côte à côte une capture du HUD et une capture de
l'interface native de Dofus fournie en référence ; vérifier la cohérence des couleurs,
formes et style d'icônes sans confusion possible avec une fenêtre Windows standard.

**Acceptance Scenarios**:

1. **Given** le HUD est affiché, **When** l'utilisateur le compare aux captures de
   référence de l'interface Dofus, **Then** la palette de couleurs, les formes de
   panneaux (coins arrondis, bordures dorées/bronze) et le style d'icônes sont
   visuellement cohérents avec ces références.
2. **Given** le HUD est affiché en superposition du jeu, **When** un observateur non
   averti le voit à l'écran, **Then** il l'identifie comme faisant partie de
   l'interface du jeu plutôt que comme une fenêtre d'application externe.

---

### Edge Cases

- Que se passe-t-il si toutes les fenêtres Dofus sont minimisées ou qu'aucune n'a le
  focus : le HUD reste-t-il affiché, se masque-t-il automatiquement ?
- Que se passe-t-il si aucune fenêtre Dofus n'est détectée au lancement de l'outil ?
- Que se passe-t-il si une fenêtre Dofus se ferme (déconnexion d'un compte) pendant
  que le HUD est affiché — le HUD doit continuer de piloter les fenêtres restantes
  sans interruption.
- Que se passe-t-il si la résolution d'écran change, ou si une fenêtre de jeu est
  redimensionnée/déplacée hors de la zone du HUD, pendant que le HUD est affiché ?
- Comment un emplacement de module vide (aucun module assigné) est-il représenté,
  pour éviter un HUD à l'aspect cassé ou incomplet ?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT afficher un HUD unique en superposition, visible
  au-dessus de l'ensemble des fenêtres Dofus ouvertes simultanément (pas un HUD par
  fenêtre/compte).
- **FR-002**: Le HUD DOIT proposer plusieurs emplacements distincts, chacun destiné à
  accueillir un module de l'outil.
- **FR-003**: L'utilisateur DOIT pouvoir afficher et masquer le HUD à tout moment sans
  interrompre le fonctionnement du reste de l'outil.
- **FR-004**: L'apparence visuelle du HUD (couleurs, formes de panneaux, style
  d'icônes) DOIT reprendre l'esthétique de l'interface native de Dofus telle
  qu'illustrée par les captures de référence.
- **FR-005**: Le HUD DOIT rester visible en permanence au-dessus de chacune des
  fenêtres Dofus ouvertes — y compris lors des changements de fenêtre active entre
  comptes — sans interrompre involontairement les interactions du joueur avec le jeu
  lui-même.
- **FR-006**: La disposition des modules dans le HUD DOIT être conservée entre deux
  lancements de l'outil.
- **FR-007**: Chaque emplacement de module DOIT afficher un état visuel distinct
  lorsqu'il n'a pas de module assigné, pour éviter un HUD à l'aspect incomplet.
- **FR-008**: Le système DOIT permettre à l'utilisateur de repositionner le HUD dans
  son ensemble sur l'écran ; la disposition interne des emplacements de modules suit un
  agencement fixe pour cette première version (pas de glisser-déposer des modules).
- **FR-009**: Le système DOIT afficher un seul HUD partagé qui pilote l'ensemble des
  fenêtres/comptes Dofus ouverts simultanément (aucune instance de HUD par fenêtre),
  et DOIT indiquer clairement quel compte est actuellement actif/ciblé lorsqu'une
  action du HUD s'applique à un compte en particulier plutôt qu'à tous.

### Key Entities

- **HUD**: La fenêtre de superposition elle-même, unique et partagée par toutes les
  fenêtres Dofus pilotées — sa position, sa visibilité, et son style visuel global.
- **Emplacement de module (slot)**: Une zone dédiée du HUD pouvant contenir un module ;
  possède une position, un état (occupé/vide), et une identité visuelle (icône/libellé).
- **Module**: Une fonctionnalité de l'outil (ex. gestion de comptes, macros) rattachée
  à un emplacement du HUD. Le contenu fonctionnel de chaque module fait l'objet de
  fonctionnalités séparées ; cette spécification couvre uniquement le contenant qui les
  accueille.
- **Disposition (layout)**: L'arrangement des emplacements de modules dans le HUD,
  persistant d'une session à l'autre.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Le HUD reste visible et correctement positionné au-dessus de toutes les
  fenêtres Dofus ouvertes pendant au moins 30 minutes d'utilisation continue, y
  compris lors de changements de fenêtre active entre comptes, sans désynchronisation
  ni disparition intempestive.
- **SC-002**: Un nouvel utilisateur identifie correctement, sans explication, à quoi
  sert chaque emplacement de module en moins de 10 secondes d'observation du HUD.
- **SC-003**: 90 % des utilisateurs testés jugent que l'apparence du HUD est cohérente
  avec l'interface de Dofus lors d'une comparaison visuelle directe.
- **SC-004**: Le HUD peut être affiché ou masqué en une seule action, avec un effet
  perçu comme instantané par l'utilisateur.
- **SC-005**: La disposition du HUD choisie par l'utilisateur est retrouvée à
  l'identique après redémarrage de l'outil dans 100 % des cas.

## Assumptions

- Cette fonctionnalité couvre uniquement le contenant (HUD + emplacements de modules
  + style visuel), pas le contenu fonctionnel de chaque module individuel — ceux-ci
  seront spécifiés séparément.
- Le HUD cible un affichage par-dessus les fenêtres de jeu Dofus en mode fenêtré ou
  fenêtré sans bordure ; le comportement en plein écran exclusif n'est pas couvert par
  cette version.
- Jusqu'à une dizaine de fenêtres Dofus (comptes) peuvent être ouvertes simultanément ;
  le HUD reste unique quel que soit ce nombre.
- Les captures d'écran fournies par l'utilisateur constituent la référence visuelle
  faisant foi pour le style du HUD (panneaux sombres, coins arrondis, bordures
  dorées/bronze, icônes rondes, compteurs hexagonaux).
- Un seul utilisateur/poste de travail à la fois ; pas de synchronisation multi-poste
  de la disposition du HUD.
