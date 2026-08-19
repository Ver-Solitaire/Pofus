# Feature Specification: Gestion des Comptes Dofus

**Feature Branch**: `002-gestion-comptes-dofus`

**Created**: 2026-08-18

**Status**: Draft

**Input**: User description: "Module de gestion des comptes Dofus dans le HUD : détection des fenêtres/comptes ouverts, activation/désactivation par compte, détection de la classe du personnage, organisation en équipes, désignation d'un leader, ordre personnalisé des comptes." Reprend le comportement du module équivalent du projet de référence (Doframe).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Voir tous mes comptes Dofus d'un coup d'œil (Priority: P1)

En tant qu'utilisateur multi-comptes, je veux que le HUD détecte automatiquement
tous mes personnages actuellement connectés à Dofus et les liste avec leur
pseudo et leur classe, afin de savoir d'un coup d'œil qui est disponible sans
devoir passer en revue chaque fenêtre.

**Why this priority**: Sans une liste fiable des comptes détectés, aucune des
fonctionnalités suivantes (activation, équipes, leader) n'a de sens — c'est le
socle du module.

**Independent Test**: Connecter plusieurs personnages à Dofus, ouvrir le
gestionnaire de comptes depuis le HUD, vérifier que chacun apparaît avec le bon
pseudo et la bonne classe, et que fermer un client Dofus retire son compte de
la liste.

**Acceptance Scenarios**:

1. **Given** deux personnages ou plus sont connectés à Dofus, **When**
   l'utilisateur ouvre le gestionnaire de comptes, **Then** chaque personnage
   apparaît avec son pseudo et sa classe.
2. **Given** un client Dofus est encore à l'écran de sélection de personnage
   (pas encore en jeu), **When** la détection s'exécute, **Then** cette fenêtre
   n'apparaît PAS comme un compte dans la liste.
3. **Given** un compte est listé, **When** son client Dofus se ferme, **Then**
   le compte disparaît de la liste sans que Pofus ne plante ni n'affiche
   d'erreur.
4. **Given** un compte est en jeu mais que sa classe ne peut pas être lue
   depuis le titre de la fenêtre, **When** il est listé, **Then** il affiche un
   état "Classe inconnue" plutôt qu'un champ vide ou une erreur.

---

### User Story 2 - Choisir quels comptes participent aux actions du HUD (Priority: P2)

En tant qu'utilisateur, je veux pouvoir activer ou désactiver individuellement
chaque compte détecté, afin d'exclure temporairement certains personnages
(observateurs, comptes en pause) des futures actions groupées du HUD (macros,
invitations...) sans avoir à fermer leur fenêtre.

**Why this priority**: C'est la première utilité concrète de la liste de
comptes — sans elle, US1 n'est qu'un affichage passif. Dépend d'US1.

**Independent Test**: Désactiver un compte, vérifier que son état "inactif" est
visible immédiatement et persiste après redémarrage de Pofus.

**Acceptance Scenarios**:

1. **Given** un compte actif est listé, **When** l'utilisateur le désactive,
   **Then** son état passe visuellement à "inactif" instantanément.
2. **Given** un compte a été désactivé, **When** Pofus redémarre et que ce
   compte est de nouveau détecté, **Then** il réapparaît toujours désactivé.
3. **Given** un compte est désactivé puis sa fenêtre Dofus est fermée puis
   rouverte, **When** il est redétecté, **Then** son état désactivé est
   conservé (l'état est associé au pseudo, pas à la fenêtre).

---

### User Story 3 - Organiser mes comptes en équipes avec un leader (Priority: P3)

En tant qu'utilisateur gérant de nombreux comptes, je veux regrouper mes
personnages en équipes nommées et désigner un leader parmi les comptes
détectés, afin de préparer des actions ciblées par équipe (à venir dans de
futurs modules) et de savoir toujours qui est le personnage "chef".

**Why this priority**: Utile pour les gros multi-comptes mais non bloquant pour
la valeur de base (US1/US2) — beaucoup d'utilisateurs avec peu de comptes n'en
auront pas l'usage immédiat.

**Independent Test**: Créer une équipe, y assigner deux comptes, désigner un
leader, redémarrer Pofus, vérifier que l'équipe et le leader sont conservés.

**Acceptance Scenarios**:

1. **Given** plusieurs comptes sont détectés, **When** l'utilisateur assigne un
   compte à une équipe nommée, **Then** cette affectation est visible dans la
   liste et persiste après redémarrage.
2. **Given** aucune équipe n'a été définie pour un compte, **When** il apparaît
   dans la liste, **Then** il est rattaché à une équipe par défaut (ex. "Équipe
   1") plutôt que de rester sans équipe.
3. **Given** plusieurs comptes sont détectés, **When** l'utilisateur désigne
   l'un d'eux comme leader, **Then** un seul compte à la fois porte le statut
   de leader, visuellement distinct des autres.
4. **Given** un compte est désigné leader, **When** l'utilisateur réordonne
   manuellement la liste des comptes actifs, **Then** le nouvel ordre est
   conservé et restauré après redémarrage, y compris pour un compte
   temporairement déconnecté puis reconnecté.

---

### Edge Cases

- Que se passe-t-il si le leader désigné ferme son client Dofus ? Le statut de
  leader doit être conservé pour son pseudo (pas perdu), prêt à se réappliquer
  à sa reconnexion — sans qu'aucun autre compte ne devienne leader
  automatiquement.
- Que se passe-t-il si plus de 50 pseudos distincts ont été vus au fil du temps
  (comptes de test, renommages) ? L'ordre personnalisé ne doit pas grossir sans
  limite : les entrées les plus anciennes de comptes actuellement non détectés
  peuvent être purgées.
- Que se passe-t-il si deux fenêtres Dofus affichent temporairement le même
  pseudo (ex. reconnexion en cours) ? Le module ne doit pas planter ; un seul
  des deux doit être retenu de façon stable jusqu'à la prochaine actualisation.
- Que se passe-t-il si aucun compte n'est détecté du tout ? Le module DOIT
  afficher un état "aucun compte détecté" explicite plutôt qu'une liste vide
  silencieuse (cohérent avec le comportement déjà établi du HUD, cf. feature
  001).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT détecter automatiquement chaque fenêtre de jeu
  Dofus correspondant à un personnage effectivement en jeu, en excluant les
  fenêtres de lancement/sélection de personnage.
- **FR-002**: Pour chaque compte détecté, le système DOIT extraire et afficher
  le pseudo du personnage et sa classe à partir des informations de la fenêtre.
- **FR-003**: Si la classe ne peut pas être déterminée, le système DOIT
  afficher un état "Classe inconnue" explicite plutôt qu'un champ vide.
- **FR-004**: Le système DOIT rafraîchir automatiquement la liste des comptes
  détectés pour refléter les fenêtres ouvertes/fermées, sans action manuelle
  requise de l'utilisateur.
- **FR-005**: L'utilisateur DOIT pouvoir activer ou désactiver individuellement
  chaque compte détecté ; cet état DOIT être conservé entre les sessions et
  associé au pseudo (pas à la fenêtre, qui change de handle à chaque
  reconnexion).
- **FR-006**: L'utilisateur DOIT pouvoir assigner chaque compte à une équipe
  nommée ; un compte sans assignation explicite appartient à une équipe par
  défaut.
- **FR-007**: L'utilisateur DOIT pouvoir désigner au plus un compte comme
  leader parmi les comptes détectés.
- **FR-008**: Le statut de leader DOIT être conservé pour le pseudo désigné
  même si son compte se déconnecte temporairement, sans transfert automatique
  du statut à un autre compte.
- **FR-009**: L'utilisateur DOIT pouvoir réordonner manuellement les comptes
  actifs ; cet ordre DOIT être conservé entre les sessions, y compris pour un
  compte temporairement déconnecté.
- **FR-010**: Le système DOIT limiter la quantité de pseudos mémorisés dans
  l'ordre personnalisé pour éviter une croissance illimitée, en purgeant en
  priorité les entrées non détectées les plus anciennes.
- **FR-011**: Le module DOIT rester pleinement utilisable, sans erreur ni
  crash, lorsqu'aucun compte Dofus n'est détecté.

### Key Entities

- **AccountSession**: Un compte Dofus actuellement détecté ou déjà connu —
  pseudo, classe, statut actif/inactif, équipe, statut leader (oui/non),
  position dans l'ordre personnalisé.
- **Team**: Un regroupement nommé de comptes, utilisé pour filtrer/cibler des
  actions futures (macros ciblées par équipe — hors périmètre de cette
  fonctionnalité).
- **AccountOrderPreference**: L'ordre personnalisé des comptes choisi par
  l'utilisateur, persistant, purgé au-delà d'une limite d'ancienneté/taille.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un compte qui vient de se connecter à Dofus apparaît dans le
  gestionnaire de comptes du HUD en moins de 5 secondes, sans action manuelle.
- **SC-002**: Activer/désactiver un compte produit un changement visuel perçu
  comme instantané.
- **SC-003**: L'état actif/inactif, l'équipe et le leader choisis par
  l'utilisateur sont restaurés à l'identique après redémarrage de Pofus dans
  100 % des cas.
- **SC-004**: Avec 8 comptes détectés, un utilisateur identifie le leader et
  la répartition en équipes en moins de 10 secondes d'observation du
  gestionnaire de comptes.

## Assumptions

- Le slot "accounts" du HUD (feature 001) sert de point d'entrée compact
  (indicateur + bouton) vers un gestionnaire de comptes dédié qui affiche la
  liste complète — la liste elle-même n'a pas vocation à tenir dans les 40×40
  px d'un emplacement de HUD. Ce choix reprend directement le "gestionnaire de
  personnages dédié" déjà présent dans le projet de référence.
- Le format de titre de fenêtre `"Pseudo - Classe"` (observé sur le client
  Dofus réel utilisé pour les tests) fait foi pour extraire pseudo et classe ;
  toute variation de format non reconnue tombe sur "Classe inconnue" plutôt que
  d'échouer.
- La notion d'équipe est purement organisationnelle dans cette fonctionnalité
  (étiquette + regroupement visuel) ; le ciblage réel d'actions par équipe est
  hors périmètre et sera couvert par les futurs modules de macros.
- Cette fonctionnalité réutilise la détection de fenêtres Dofus déjà en place
  (feature 001) comme source brute, en y ajoutant l'extraction pseudo/classe et
  l'état persistant par compte.
