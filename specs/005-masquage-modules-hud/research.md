# Research: Masquage des fenêtres de Pofus

> Révisé le 2026-08-19 avec le périmètre corrigé (fenêtres, pas boutons de
> module). Les décisions #2 et #4 sont inchangées ; #1 et #3 ont été reprises.

## 1. Où stocker l'état masqué/affiché d'une fenêtre

**Decision**: Un nouveau fichier `%APPDATA%\Pofus\panels.json`, portant pour
chaque fenêtre son état masqué/affiché **et** son raccourci de réaffichage
(`PanelSettings`).

**Rationale**: Les deux informations sont toujours lues et écrites ensemble
(l'écran des raccourcis affiche l'état, le masquage consulte le raccourci) :
les séparer en deux fichiers n'apporterait qu'un risque d'incohérence. Le
fichier reste distinct de `navigation-shortcuts.json` pour ne pas toucher au
format déjà en production des raccourcis de navigation.

**Alternatives considered**:
- Étendre `HudLayout`/`hud-layout.json` : rejeté — ce modèle décrit le
  contenu *interne* de la barre HUD (slots, position), pas l'ensemble des
  fenêtres de l'application ; y loger l'état du widget des personnages serait
  un abus de responsabilité.
- Réutiliser `SwitcherWidgetPreferences` pour le widget et `HudLayout` pour
  le HUD : rejeté — deux mécanismes pour une même préoccupation, et rien de
  générique pour les fenêtres futures (FR-012).

## 1bis. Constat de validation — trois pièges découverts à l'exécution

Trois défauts n'ont été révélés que par la validation en conditions réelles,
et sont documentés ici parce qu'ils reviendront pour toute fenêtre future :

1. **Nos propres popups étaient recouverts.** `HudWindow` et
   `AccountSwitcherWidget` réaffirmaient `HWND_TOPMOST` à chaque changement
   de premier plan, y compris quand le nouveau premier plan était un popup de
   Pofus lui-même : le menu contextuel s'ouvrait (l'événement
   `ContextMenuOpening` se déclenchait bien) mais restait invisible. Corrigé
   en ignorant les fenêtres de notre propre processus.
2. **Les raccourcis étaient enregistrés avant d'être chargés.**
   `AttachShortcuts` s'exécute depuis `Loaded` de la fenêtre hôte, qui se
   déclenche pendant `Show()` — donc avant le chargement asynchrone des
   préférences. D'où la séparation `LoadAsync()` (avant `Show`) /
   `ApplyPersistedState()` (après).
3. **Un hotkey enregistré est invisible pour l'UI de capture.** Windows
   délivre une combinaison enregistrée via `RegisterHotKey` en `WM_HOTKEY` à
   la fenêtre qui l'a enregistrée, jamais comme frappe normale à la fenêtre
   ayant le focus. Sans suspension, réassigner une combinaison déjà prise
   laissait la capture bloquée *et* déclenchait l'action liée. D'où
   `SuspendAll`/`ResumeAll` autour du mode capture.

## 2. Généraliser le mécanisme de raccourci global

**Decision**: Généraliser `GlobalHotkeyListener` (feature 003) pour qu'il
identifie chaque raccourci par une clé `string` (`actionId`) plutôt que par
`NavigationAction`, avec une table interne `Dictionary<string, int>`
attribuant un identifiant `RegisterHotKey` stable et unique par action
enregistrée (compteur interne), au lieu de caster l'enum en `int`.
`NavigationHudModule` fait la correspondance `NavigationAction <-> "nav:*"`
à sa propre frontière ; les nouvelles actions de module utilisent `"module:*"`.

**Rationale**: `RegisterHotKey` exige un entier unique par combinaison
enregistrée au niveau du processus Windows. L'implémentation actuelle
caste `(int)NavigationAction`, ce qui fonctionne uniquement parce que le jeu
d'actions est un enum fermé et petit. Les modules masquables forment un
ensemble ouvert (accounts, macros, settings, et les futurs slots) identifié
par un `string` (`IHudModule.ModuleId`), donc la clé de dispatch doit
devenir generic/string. Une table interne compteur évite tout risque de
collision entre les ID `RegisterHotKey` attribués aux actions de navigation
et ceux des actions de module — contrairement à une approche par hash de
chaîne, qui resterait un risque de collision silencieuse.

**Alternatives considered**:
- Deuxième instance séparée de `GlobalHotkeyListener` dédiée aux modules :
  rejeté — duplique l'installation du hook `HwndSource.AddHook` sur la même
  fenêtre pour la même préoccupation (raccourcis globaux), sans bénéfice ;
  complique aussi la détection de conflit inter-domaines (FR-006) qui doit
  justement voir *toutes* les combinaisons déjà prises, navigation incluse.
- Étendre `NavigationAction` avec des valeurs dynamiques : impossible, un
  enum C# est un ensemble fermé à la compilation — incompatible avec un
  ensemble de modules ouvert/extensible à l'exécution.

## 3. Affordance pour masquer une fenêtre

**Decision**: Clic droit sur la fenêtre → menu contextuel à une seule entrée,
« Masquer cette fenêtre » (choix de l'utilisateur), attaché de façon
générique par `PanelContextMenu.Attach`.

**Rationale**: Répond à SC-001 (une seule action directe) sans ajouter de
chrome permanent sur des barres voulues compactes. Attacher le menu à la
`Window` elle-même plutôt qu'à un contrôle interne le rend valable pour
n'importe quelle fenêtre future sans code dédié (FR-012). Le thème fournit
désormais des styles `ContextMenu`/`MenuItem` pour que ces menus ne retombent
pas sur le chrome Windows par défaut.

**Alternatives considered**:
- Raccourci unique faisant bascule (afficher/masquer) : écarté par
  l'utilisateur, qui préfère un clic droit explicite pour masquer.
- Petit bouton « × » permanent sur chaque fenêtre : rejeté — clutter visuel
  constant sur des barres dont la compacité est un objectif du HUD.

**Piège associé** : sur ces fenêtres `Topmost` + `AllowsTransparency`, le
menu ne s'affichait pas tant que la fenêtre réaffirmait son topmost au
passage au premier plan de son propre popup (cf. #1bis).

## 4. Détection de conflit entre raccourcis de navigation et de module

**Decision**: `NavigationShortcutsWindow` (étendue) vérifie chaque nouvelle
combinaison capturée contre **les deux** ensembles de liaisons déjà pris
(`NavigationShortcutPreferences.Bindings` et
`ModuleShortcutPreferences.Bindings`) avant de l'appliquer, via une
vérification unifiée au moment de la capture — pas de nouveau service Core
séparé, la fenêtre a déjà les deux objets de préférences chargés en
mémoire.

**Rationale**: FR-006 exige qu'aucun conflit ne soit appliqué
silencieusement, y compris entre les deux domaines (un raccourci de module
ne doit pas silencieusement remplacer un raccourci de navigation, et
inversement). Comme la fenêtre de capture est déjà le point unique où une
nouvelle combinaison est proposée par l'utilisateur, c'est le point naturel
et suffisant pour cette vérification — inutile d'introduire un service Core
dédié pour une logique de comparaison aussi simple (itérer deux
dictionnaires et comparer des `KeyCombo`).

**Alternatives considered**:
- Vérification uniquement côté `GlobalHotkeyListener` au moment de
  `RegisterHotKey` : rejeté — à ce stade, un échec de `RegisterHotKey`
  (Win32 error) ne distingue pas « déjà pris par Pofus lui-même » de
  « déjà pris par un autre logiciel » ; la détection de conflit *avant*
  d'appeler `RegisterHotKey` (FR-006) donne un message utilisateur
  beaucoup plus clair (« déjà utilisé par tel raccourci Pofus » vs échec
  Win32 générique).
