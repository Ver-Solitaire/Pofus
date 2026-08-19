# Quickstart: Paramètres — Détection de Conflits et Démarrage

## Prérequis

- Windows 10/11, .NET 10 SDK
- Un exécutable nommé `organizer.exe` (peut être n'importe quel binaire vide
  renommé, juste pour tester la détection par nom de processus)

## Build et lancement

```powershell
dotnet build
dotnet run --project src\Pofus.App
```

## Scénarios de validation

### 1. Détection de conflit au démarrage (US1 / FR-001 à FR-005, FR-010)

1. Lancer un exécutable nommé `organizer.exe`.
2. Lancer Pofus.
3. **Attendu** : un avertissement apparaît en moins de 5 secondes, nommant le
   logiciel détecté (SC-001).
4. Cliquer "Fermer le logiciel concurrent".
5. **Attendu** : le processus `organizer.exe` se termine, l'avertissement se
   referme (SC-002).
6. Relancer `organizer.exe`, relancer Pofus, cette fois cliquer "Continuer
   sans fermer" puis "Ne plus m'avertir".
7. Fermer et relancer Pofus avec `organizer.exe` toujours actif.
8. **Attendu** : aucun avertissement n'apparaît (SC-004).
9. Fermer `organizer.exe`, relancer Pofus.
10. **Attendu** : aucun avertissement (rien à détecter), démarrage normal
    (FR-010).

### 2. Lancement automatique au démarrage de Windows (US2 / FR-007, FR-008)

1. Ouvrir les réglages généraux depuis le slot "settings" du HUD.
2. Activer le lancement automatique.
3. Vérifier dans le registre
   (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) qu'une entrée Pofus
   a été créée.
4. Désactiver le lancement automatique.
5. **Attendu** : l'entrée de registre est supprimée.

### 3. Réinitialisation de l'avertissement (US3 / FR-009)

1. Avec l'avertissement précédemment ignoré définitivement (scénario 1, étape
   6-8), ouvrir les réglages généraux.
2. Cliquer "Réinitialiser l'avertissement de conflit".
3. Relancer Pofus avec `organizer.exe` actif.
4. **Attendu** : l'avertissement réapparaît (SC-004 inversé).

### 4. Cas limites

- Tenter de fermer `organizer.exe` alors qu'il est protégé/inaccessible :
  message d'échec explicite, pas de plantage (FR-011).
- Activer le lancement automatique sans les droits suffisants sur le
  registre : message d'échec explicite, état non modifié silencieusement
  (FR-011).

## Critères de sortie

- [ ] Les 4 scénarios ci-dessus passent sans contournement manuel.
- [ ] Aucune exception non gérée dans les logs pendant les scénarios.
