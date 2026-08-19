# Quickstart: Gestion des Comptes Dofus

## Prérequis

- Windows 10/11, .NET 10 SDK
- Au moins un client Dofus connecté à un personnage (idéalement 2+ pour tester
  équipes/leader/ordre)

## Build et lancement

```powershell
dotnet build
dotnet run --project src\Pofus.App
```

## Scénarios de validation

### 1. Détection et affichage (US1 / FR-001 à FR-004)

1. Connecter un ou plusieurs personnages à Dofus.
2. Cliquer sur le slot "accounts" du HUD.
3. **Attendu** : le gestionnaire de comptes s'ouvre, liste chaque personnage
   avec pseudo + classe, en moins de 5 secondes après connexion (SC-001).
4. Rester sur l'écran de sélection de personnage sans entrer en jeu.
5. **Attendu** : cette fenêtre n'apparaît PAS comme un compte.
6. Fermer un client Dofus listé.
7. **Attendu** : le compte disparaît de la liste sans erreur.

### 2. Activation/désactivation (US2 / FR-005)

1. Désactiver un compte dans le gestionnaire.
2. **Attendu** : changement visuel instantané (SC-002).
3. Redémarrer Pofus, reconnecter le même compte.
4. **Attendu** : il réapparaît toujours désactivé (SC-003).

### 3. Équipes et leader (US3 / FR-006 à FR-009)

1. Assigner deux comptes à une équipe nommée.
2. Désigner un des comptes comme leader.
3. Réordonner les comptes actifs via les boutons Monter/Descendre.
4. Redémarrer Pofus.
5. **Attendu** : équipe, leader et ordre sont restaurés à l'identique
   (SC-003).
6. Fermer le client du leader.
7. **Attendu** : le statut de leader n'est pas transféré à un autre compte ; il
   se réapplique si le même pseudo se reconnecte.

### 4. Cas limites

- Lancer le gestionnaire sans aucun compte détecté : état "aucun compte"
  explicite (FR-011), pas de plantage.
- Avec 8 comptes détectés, un observateur identifie le leader et les équipes
  en moins de 10 secondes (SC-004).

## Critères de sortie

- [ ] Les 4 scénarios ci-dessus passent sans contournement manuel.
- [ ] Aucune exception non gérée dans les logs pendant les scénarios.
