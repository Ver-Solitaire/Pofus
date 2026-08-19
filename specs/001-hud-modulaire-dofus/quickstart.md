# Quickstart: HUD Modulaire Style Dofus

Guide de validation manuelle de bout en bout pour cette fonctionnalité. À exécuter
après `/speckit-implement`, avant de considérer la fonctionnalité terminée.

## Prérequis

- Windows 10/11
- .NET 10 SDK installé
- Le client Dofus installé et lançable (fenêtré ou fenêtré sans bordure — le
  plein écran exclusif est hors périmètre, voir Assumptions de la spec)
- Les captures d'écran de référence de l'interface Dofus (fournies par
  l'utilisateur) à portée de main pour la comparaison visuelle

## Build et lancement

```powershell
dotnet build
dotnet run --project src/Pofus.App
```

## Scénarios de validation

### 1. Affichage du HUD au-dessus du jeu (US1 / FR-001, FR-005)

1. Lancer Dofus et se connecter à un compte.
2. Lancer Pofus.
3. **Attendu** : le HUD apparaît en superposition, au-dessus de la fenêtre de
   jeu, sans voler le focus clavier au jeu.
4. Jouer normalement pendant quelques minutes (déplacements, clics, ouverture de
   menus en jeu).
5. **Attendu** : le HUD reste visible et positionné correctement (SC-001) ; les
   interactions avec le jeu en dehors de la zone du HUD ne sont pas bloquées.

### 2. Masquage/affichage du HUD (US1 / FR-003, SC-004)

1. Déclencher l'action de masquage du HUD.
2. **Attendu** : disparition perçue comme instantanée, sans fermer Pofus.
3. Déclencher l'action d'affichage.
4. **Attendu** : réapparition immédiate, à la même position qu'avant masquage.

### 3. Emplacements de modules et disposition fixe (US2 / FR-002, FR-006, FR-007, FR-008)

1. Observer le HUD : chaque emplacement de module doit être visuellement
   délimité, avec une icône/libellé identifiable (SC-002 : identifiable en moins
   de 10 secondes sans explication).
2. Vérifier qu'un emplacement sans module assigné affiche un état visuel
   distinct "vide" (pas de case blanche/cassée).
3. Déplacer le HUD à un autre endroit de l'écran.
4. Fermer puis relancer Pofus.
5. **Attendu** : la position du HUD est restaurée à l'identique (SC-005) ; la
   disposition des emplacements reste fixe (pas de réagencement possible en v1).

### 4. Esthétique Dofus (US3 / FR-004)

1. Placer côte à côte une capture du HUD et les captures de référence Dofus.
2. **Attendu** : cohérence visuelle des couleurs, coins arrondis, bordures
   dorées/bronze, style d'icônes rondes (SC-003).

### 5. HUD unique au-dessus de toutes les fenêtres multi-comptes (FR-001, FR-005, FR-009)

1. Ouvrir plusieurs fenêtres Dofus (idéalement 4 à 8) avec des comptes différents.
2. **Attendu** : un seul HUD Pofus s'affiche (pas une instance par fenêtre).
3. Faire passer chaque fenêtre Dofus au premier plan tour à tour (clic sur la
   fenêtre, alt-tab).
4. **Attendu** : le HUD reste visible au-dessus de la fenêtre nouvellement active
   à chaque changement, sans clignoter, disparaître ni se dupliquer ; sa position
   à l'écran ne change pas (il n'est pas ancré à une fenêtre en particulier).
5. **Attendu** : l'indicateur de compte actif du HUD se met à jour pour refléter
   la fenêtre au premier plan.
6. Fermer une des fenêtres Dofus (simulation de déconnexion d'un compte).
7. **Attendu** : le HUD continue de piloter les fenêtres restantes sans erreur ni
   interruption.

### 6. Cas limites

- Lancer Pofus **sans** qu'aucune fenêtre Dofus ne soit ouverte : le HUD DOIT
  gérer ce cas explicitement (pas de crash, état "aucun compte détecté" visible)
  plutôt que d'avaler l'erreur silencieusement (Principe I).
- Minimiser la fenêtre Dofus : vérifier le comportement du HUD (reste affiché ou
  se masque selon le comportement implémenté) et confirmer qu'il correspond à un
  comportement documenté, pas à un état incohérent.
- Redimensionner la fenêtre Dofus ou changer de résolution d'écran pendant que
  le HUD est affiché : le HUD ne doit pas se retrouver hors écran ou déformé.

## Critères de sortie

- [ ] Les 6 scénarios ci-dessus passent sans intervention manuelle de
      contournement.
- [ ] Aucune exception non gérée observée dans les logs pendant les scénarios
      (Principe I — Robustesse).
- [ ] Comparaison visuelle avec les captures de référence jugée cohérente
      (SC-003).
