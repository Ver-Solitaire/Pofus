# Pofus

Assistant multi-comptes pour Dofus, sous Windows : un HUD qui reste au-dessus
du jeu, la gestion des personnages connectés, la navigation au clavier entre
fenêtres, et une liste de courses de craft.

Écrit en C# / .NET 10 + WPF, développé avec
[spec-kit](https://github.com/github/spec-kit) (Spec-Driven Development).

## Installation

Téléchargez `Pofus-1.0.0-setup.exe` depuis la
[dernière version](../../releases/latest) et lancez-le. L'installation se fait
par utilisateur : ni droits administrateur, ni prérequis à installer (le
runtime .NET est inclus).

## Fonctionnalités

**HUD** — une barre unique flottant au-dessus de **toutes** les fenêtres Dofus
à la fois (pas une par fenêtre), déplaçable, dont la position est conservée.
Chaque emplacement accueille un module indépendant ; le plantage d'un module
n'emporte pas le HUD.

**Comptes** — détection automatique des personnages connectés (pseudo et
classe) à partir des fenêtres du jeu. Activation par personnage, équipes,
leader, réordonnancement. Un widget détachable liste les personnages avec leur
icône de classe, entoure celui qui a le focus, et signale le leader par un halo
irisé.

**Navigation** — raccourcis clavier globaux pour passer au personnage suivant,
précédent, ou au leader, depuis n'importe quelle fenêtre. Réassignables, avec
détection des conflits.

**Atelier** — collez le lien d'un atelier DofusBook : Pofus en tire la liste
des ressources nécessaires, quantités additionnées quand plusieurs équipements
partagent une ressource. Clic sur un nom pour le copier, case à cocher par
ressource, progression conservée.

**Apparence** — couleur et opacité du fond, du texte et des bordures,
appliquées en direct à toutes les fenêtres, avec préréglages.

**Masquage** — chaque fenêtre se masque d'un clic droit et revient par un
raccourci qui lui est propre. L'icône de la zone de notification reste le point
d'entrée garanti.

## Vie privée

Toute la configuration reste locale, dans `%APPDATA%\Pofus`. La seule sortie
réseau est la lecture d'un atelier DofusBook, déclenchée uniquement quand vous
cliquez « Importer » ; aucune donnée personnelle n'est transmise, et Pofus ne
demande jamais d'identifiants de jeu.

## Développement

```powershell
dotnet build          # nécessite le SDK .NET 10
dotnet test
dotnet run --project src\Pofus.App
```

### Structure

```text
src/
├── Pofus.App/       # Composition root WPF
├── Pofus.Hud/       # Fenêtres, contrôles, thème
├── Pofus.Core/      # Modèles et logique pure — testables sans Windows
└── Pofus.Platform/  # Interop Win32 et accès système

tests/
├── Pofus.Core.Tests/
└── Pofus.Platform.Tests/

specs/               # Une spécification par fonctionnalité (spec-kit)
installer/           # Script Inno Setup
```

La séparation Core / Platform / Hud est délibérée : `Pofus.Core` ne dépend ni
de WPF ni de Win32, ce qui rend la logique métier testable sans session
Windows. Voir [`.specify/memory/constitution.md`](.specify/memory/constitution.md)
pour les principes du projet, et `specs/` pour le détail des décisions de
conception.

### Construire l'installeur

```powershell
dotnet publish src\Pofus.App\Pofus.App.csproj -c Release -r win-x64 --self-contained true -o publish
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\Pofus.iss
```

## Changelog

Voir [CHANGELOG.md](CHANGELOG.md).
