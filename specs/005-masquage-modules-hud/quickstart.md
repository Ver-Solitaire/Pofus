# Quickstart — Validation : Masquage des fenêtres de Pofus

## Prérequis

- Windows 10/11, .NET 10 SDK
- Au moins une fenêtre Dofus ouverte (pour peupler le widget des personnages)

## Build & tests

```powershell
dotnet build Pofus.slnx
dotnet test Pofus.slnx
```

Attendu : build sans avertissement, tous les tests au vert (aucune régression
sur les features 001-004).

## Lancer l'application

```powershell
dotnet run --project src/Pofus.App/Pofus.App.csproj
```

---

## Scénario 1 — Masquer une fenêtre (US1, FR-001/FR-002/FR-003)

1. Clic droit sur le **widget des personnages** → « Masquer cette fenêtre ».

**Attendu** : le widget disparaît immédiatement ; la barre HUD reste visible
et fonctionnelle ; la détection des comptes et les raccourcis de navigation
continuent de fonctionner.

2. Vérifier `%APPDATA%\Pofus\panels.json` : `"switcher"` porte
   `"IsHidden": true`.

## Scénario 2 — Persistance au redémarrage (FR-007)

1. Quitter Pofus (menu de la zone de notification → Quitter), puis relancer.

**Attendu** : le widget est toujours masqué au démarrage, sans action de
l'utilisateur.

## Scénario 3 — Repli garanti par la zone de notification (FR-009)

1. Ouvrir le menu de l'icône Pofus dans la zone de notification.

**Attendu** : chaque fenêtre masquable est listée avec une coche indiquant si
elle est visible ; cliquer sur une entrée masque ou réaffiche la fenêtre
correspondante. C'est le point d'entrée garanti, même sans aucun raccourci
configuré.

## Scénario 4 — Attribuer un raccourci et détecter les conflits (US3, FR-004/FR-006/FR-008)

1. Barre HUD → bouton « Nav » → la fenêtre « Raccourcis » s'ouvre.
2. Section « Réafficher une fenêtre » : « Modifier » en face du **widget des
   personnages**, puis appuyer sur `Ctrl+Alt+A`.

**Attendu** : la liaison est acceptée et affichée.

3. « Modifier » en face de la **barre HUD**, puis appuyer sur `Ctrl+Alt+A`.

**Attendu** : « Combinaison déjà utilisée par « réafficher Widget des
personnages » » ; la liaison de la barre HUD n'est pas modifiée.

4. « Modifier » en face de la barre HUD, puis appuyer sur un raccourci de
   navigation existant (ex. `Ctrl+L`).

**Attendu** : conflit signalé également (détection croisée navigation ↔
fenêtres). Les hotkeys globaux sont suspendus pendant la capture, donc
l'action de navigation ne se déclenche **pas** pendant la saisie.

## Scénario 5 — Réafficher par raccourci global (US2, FR-004/FR-005/FR-010)

1. Widget masqué, `Ctrl+Alt+A` attribué.
2. Donner le focus à une **fenêtre Dofus**, appuyer sur `Ctrl+Alt+A`.

**Attendu** : le widget réapparaît en moins d'une seconde, **à la position
qu'il avait avant d'être masqué**, sans que le focus quitte Dofus.

3. Appuyer à nouveau sur `Ctrl+Alt+A` alors qu'il est déjà visible.

**Attendu** : aucun effet, aucune erreur (idempotent).

## Scénario 6 — Toutes les fenêtres masquées (Edge case)

1. Masquer la barre HUD **et** le widget.

**Attendu** : Pofus continue de tourner ; l'icône de la zone de notification
reste présente et permet de tout réafficher.

## Scénario 7 — Raccourci impossible à enregistrer (FR-011)

Difficile à provoquer de façon déterministe (dépend des autres logiciels
installés). Si un raccourci échoue à s'enregistrer :

**Attendu** : un avertissement explicite dans
`%APPDATA%\Pofus\logs\pofus.log` (« RegisterHotKey failed … ») ; Pofus
continue de fonctionner ; la fenêtre reste réaffichable depuis la zone de
notification.

---

## Nettoyage après validation

- Réafficher toutes les fenêtres masquées pendant le test.
- Pour repartir d'un état neuf, supprimer `%APPDATA%\Pofus\panels.json`.
