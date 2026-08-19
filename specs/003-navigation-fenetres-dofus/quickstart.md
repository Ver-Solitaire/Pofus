# Quickstart: Navigation Rapide Entre Fenêtres Dofus

## Prérequis

- Windows 10/11, .NET 10 SDK
- Au moins deux clients Dofus connectés à des personnages (idéalement 3+, avec
  un leader désigné via le module Gestion des Comptes — feature 002)

## Build et lancement

```powershell
dotnet build
dotnet run --project src\Pofus.App
```

## Scénarios de validation

### 1. Navigation suivant/précédent (US1 / FR-001, FR-002, FR-004, FR-009, FR-010)

1. Connecter 3 comptes à Dofus, tous actifs.
2. Donner le focus à une fenêtre Dofus (pas à Pofus).
3. Appuyer sur le raccourci "compte suivant" (`Ctrl+Tab` par défaut).
4. **Attendu** : la fenêtre du compte suivant dans l'ordre passe au premier
   plan en moins d'une seconde (SC-001).
5. Répéter jusqu'à revenir au premier compte (boucle, scénario 2 de la spec).
6. Désactiver un compte depuis le gestionnaire de comptes (feature 002)
   pendant la navigation.
7. **Attendu** : ce compte n'apparaît plus dans le cycle suivant/précédent dès
   la prochaine navigation.
8. Changer manuellement de fenêtre Dofus (clic direct, pas de raccourci), puis
   utiliser "compte suivant".
9. **Attendu** : la navigation reprend à partir de la fenêtre réellement
   affichée, pas de la dernière position mémorisée par le raccourci.

### 2. Aller au leader (US2 / FR-003, FR-008)

1. Désigner un leader (feature 002).
2. Depuis un autre compte actif, déclencher le raccourci "aller au leader"
   (`Ctrl+L` par défaut).
3. **Attendu** : la fenêtre du leader passe au premier plan.
4. Retirer la désignation de leader, déclencher à nouveau le raccourci.
5. **Attendu** : rien ne se produit, aucune erreur.

### 3. Reconfiguration des raccourcis (US3 / FR-005, FR-006, FR-007)

1. Ouvrir la configuration des raccourcis depuis le slot "macros" du HUD.
2. Ré-assigner "compte précédent" à une nouvelle combinaison.
3. **Attendu** : la nouvelle combinaison fonctionne immédiatement (SC-002),
   sans redémarrer Pofus.
4. Essayer d'assigner à "aller au leader" la même combinaison que "compte
   suivant".
5. **Attendu** : le système refuse et indique le conflit.
6. Redémarrer Pofus.
7. **Attendu** : les raccourcis personnalisés sont conservés à l'identique
   (SC-003).

### 4. Cas limites

- Un seul compte actif : "suivant"/"précédent" ne provoquent aucun changement
  visible ni erreur.
- Aucun compte actif détecté : les raccourcis ne font rien, sans erreur
  (FR-008).
- 8 comptes actifs : parcourir l'ensemble au clavier seul, sans souris
  (SC-004).

## Critères de sortie

- [ ] Les 4 scénarios ci-dessus passent sans contournement manuel.
- [ ] Aucune exception non gérée dans les logs pendant les scénarios.
