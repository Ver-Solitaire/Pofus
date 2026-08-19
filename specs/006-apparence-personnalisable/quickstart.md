# Quickstart — Validation : Apparence personnalisable

## Prérequis

- Windows 10/11, .NET 10 SDK
- Idéalement une fenêtre Dofus ouverte derrière, pour juger la transparence

## Build & tests

```powershell
dotnet build Pofus.slnx
dotnet test Pofus.slnx
```

## Lancer

```powershell
dotnet run --project src/Pofus.App/Pofus.App.csproj
```

---

## Scénario 1 — Transparence en direct (US1, FR-001/FR-005)

1. Barre HUD → bouton « ⚙ » → section **Apparence**.
2. Baisser le curseur **opacité du fond**.

**Attendu** : le HUD, le widget des personnages **et** la fenêtre de réglages
elle-même deviennent translucides au fur et à mesure du déplacement, sans
redémarrage. Le décor de Dofus se lit derrière.

3. Ouvrir en plus le gestionnaire de comptes.

**Attendu** : il adopte la même apparence (le thème est global, FR-004).

## Scénario 2 — Persistance (FR-007)

1. Quitter Pofus, vérifier `%APPDATA%\Pofus\appearance.json`, relancer.

**Attendu** : l'apparence réglée est restaurée au démarrage.

## Scénario 3 — Couleurs (US2, FR-002/FR-003)

1. Changer la couleur des **bordures** (champ hexadécimal ou nuancier).

**Attendu** : seules les bordures changent ; fond et texte inchangés.

2. Changer la couleur du **texte**.

**Attendu** : les libellés de toutes les fenêtres suivent. Le halo du leader,
le liseré blanc de focus et les couleurs de classe **ne changent pas** (ce
sont des repères fonctionnels).

## Scénario 4 — Préréglages (US3, FR-006)

1. Cliquer **« Verre translucide »**.

**Attendu** : fond, texte et bordures changent d'un coup.

2. Ajuster ensuite l'opacité du fond.

**Attendu** : seule cette valeur bouge, le reste du préréglage est conservé.

## Scénario 5 — Plancher d'opacité et réinitialisation (FR-008/FR-009)

1. Pousser l'opacité du fond au **minimum**.

**Attendu** : les fenêtres restent visibles (jamais totalement transparentes)
et saisissables à la souris — impossible de « perdre » l'interface.

2. Cliquer **« Réinitialiser »**.

**Attendu** : retour immédiat à l'apparence par défaut, en une seule action.

## Scénario 6 — Fichier corrompu (FR-010)

1. Quitter Pofus, remplacer le contenu de `appearance.json` par `{ oops`,
   relancer.

**Attendu** : Pofus démarre avec l'apparence par défaut ; une ligne `[ERROR]`
explicite figure dans `%APPDATA%\Pofus\logs\pofus.log`.

---

## Nettoyage

- « Réinitialiser », ou supprimer `%APPDATA%\Pofus\appearance.json`.
