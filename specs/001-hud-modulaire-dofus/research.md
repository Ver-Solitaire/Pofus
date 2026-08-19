# Research: HUD Modulaire Style Dofus

## Decision: Langage et framework — C# / .NET 10 + WPF

**Rationale**: L'utilisateur a choisi C#/.NET pour la fluidité de rendu native
Windows, en remplacement de la stack Python/customtkinter de Doframe (le projet de
référence), jugée trop limitée pour un overlay transparent réactif. .NET 10
(LTS, support jusqu'à ~2028) est retenu plutôt que .NET 8 : au moment de
l'implémentation, .NET 8 n'avait plus que quelques mois de support LTS restants
(fin nov. 2026), alors que .NET 10 est la LTS courante disponible.

Entre WPF et WinUI 3 (les deux options .NET pour une UI desktop moderne) : **WPF**
est retenu.
- WPF gère nativement les fenêtres calques transparentes et le click-through
  sélectif via `AllowsTransparency="True"` + `WindowStyle="None"` côté managé, et
  via les styles étendus Win32 (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`) en P/Invoke
  pour un contrôle fin zone par zone.
- WPF supporte un always-on-top fiable (`Topmost="True"` + réassertion via
  `SetWindowPos(HWND_TOPMOST)`), un pattern éprouvé pour les overlays de jeu.
- WinUI 3 a un support significativement plus fragile et moins documenté pour la
  transparence par pixel et le click-through partiel au-dessus d'une autre
  application — risque trop élevé pour le socle même de cette fonctionnalité.

**Alternatives considered**:
- **WinUI 3**: rejeté pour la fragilité de la transparence/click-through partiel.
- **Python + PySide6/PyQt6**: écarté par le choix explicite de l'utilisateur de
  changer d'écosystème pour maximiser la fluidité native Windows.
- **Electron**: écarté, empreinte mémoire et latence de rendu incompatibles avec
  Principe II (Fluidité et performance mesurées) de la constitution.

## Decision: Maintien au premier plan au-dessus de TOUTES les fenêtres Dofus

**Rationale**: Le HUD est unique et partagé — il ne suit pas une fenêtre Dofus en
particulier, il doit dominer le z-order au-dessus de chacune des fenêtres Dofus
ouvertes (jusqu'à une dizaine de comptes), quelle que soit celle actuellement au
premier plan (correction utilisateur : "pas un HUD par compte, un seul HUD
au-dessus des ~8 fenêtres qui les pilote toutes"). `Topmost="True"` seul peut être
repris par certaines fenêtres de jeu lors d'un changement de focus. La fenêtre HUD
DOIT donc :
1. Utiliser `SetWindowPos` avec `HWND_TOPMOST` au lancement et à chaque
   changement de fenêtre au premier plan, **quelle que soit la fenêtre concernée**
   (pas seulement une fenêtre Dofus "suivie" en particulier) — puisque n'importe
   laquelle des N fenêtres Dofus (ou une autre application) peut tenter de
   repasser au-dessus.
2. S'abonner aux évènements de changement de fenêtre au premier plan via
   `SetWinEventHook` (`EVENT_SYSTEM_FOREGROUND`), sans filtrer sur un processus
   Dofus précis, plutôt que du polling, pour rester réactif sans consommer de CPU
   en continu (cohérent avec Principe II).
3. Le HUD garde une position fixe à l'écran, indépendante de la position ou de la
   fenêtre Dofus actuellement active (pas d'ancrage à une fenêtre de jeu
   particulière).

**Alternatives considered**:
- **Polling périodique de la fenêtre active** : rejeté, gaspille du CPU en continu
  pour un gain de réactivité inférieur à un hook d'évènement.
- **Ancrer le HUD à la fenêtre Dofus actuellement active** : rejeté explicitement
  par l'utilisateur — le HUD doit avoir une position propre, stable, au-dessus de
  toutes les fenêtres à la fois, pas suivre celle qui a le focus.

## Decision: Détection des fenêtres Dofus (plurielles)

**Rationale**: Détection par nom de processus (`Dofus.exe` / variantes du
launcher) via `EnumWindows` + `GetWindowThreadProcessId`, encapsulée dans un
module Win32 dédié (Principe IV : intégration Windows fiable). La détection
retourne la **liste complète** des fenêtres Dofus actuellement ouvertes (pas une
seule "fenêtre suivie") ; c'est cette liste qui alimente l'indicateur de compte
actif (FR-009) quand une action du HUD cible un compte précis. Contrairement à
l'implémentation de référence (Doframe) qui avale les erreurs (`except: pass`),
chaque échec de détection DOIT être explicite : état "aucune fenêtre Dofus
trouvée" propagé et géré par le HUD (cf. Edge Cases de la spec), jamais une
exception silencieuse.

**Alternatives considered**:
- **Détection par titre de fenêtre uniquement** : rejeté, trop fragile (le titre
  peut varier selon la langue/version du client).
- **Suivre une seule fenêtre "principale"** : rejeté — le HUD doit connaître
  toutes les fenêtres Dofus ouvertes simultanément pour les piloter ensemble.

## Decision: Persistance de la disposition du HUD

**Rationale**: Un fichier JSON local (`%APPDATA%\Pofus\hud-layout.json`), lu/écrit
via `System.Text.Json`, stocke la position du HUD à l'écran et l'état de chaque
emplacement de module. Cohérent avec Principe V (données locales uniquement,
aucune transmission réseau) et FR-006/SC-005 (persistance entre sessions).
Écriture asynchrone pour ne jamais bloquer le thread d'interface (Principe II).

**Alternatives considered**:
- **Base de données locale (SQLite)** : rejeté, sur-dimensionné pour une poignée de
  valeurs de configuration ; complexité non justifiée (YAGNI).

## Decision: Stratégie de test

**Rationale**: La logique métier (calcul de disposition, persistance, détection de
fenêtre) est isolée dans des classes/services testables indépendamment du rendu
WPF (Principe III), couverts par des tests unitaires **xUnit**. Le rendu visuel et
le comportement d'overlay réel (always-on-top, transparence) ne sont pas
unit-testables de façon fiable : ils sont validés manuellement via le
`quickstart.md` (Phase 1), qui inclut une comparaison directe avec les captures de
référence Dofus.

**Alternatives considered**:
- **Tests UI automatisés (FlaUI, WinAppDriver)** : jugé disproportionné pour cette
  première fonctionnalité ; à reconsidérer si des régressions visuelles
  deviennent fréquentes.

## Outstanding NEEDS CLARIFICATION

Aucune — tous les inconnus techniques du Technical Context ont été résolus
ci-dessus.
