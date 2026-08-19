# Research: Navigation Rapide Entre Fenêtres Dofus

## Decision: Raccourcis globaux via `RegisterHotKey`

**Rationale**: `RegisterHotKey`/`UnregisterHotKey` (user32.dll) enregistrent une
combinaison de touches au niveau système : Windows poste un message
`WM_HOTKEY` à la fenêtre enregistrante quel que soit le premier plan actuel —
exactement le besoin de FR-009 (fonctionne même quand une fenêtre Dofus a le
focus). Bénéfice secondaire : Windows refuse d'enregistrer deux fois la même
combinaison (même par le même processus), ce qui couvre FR-006 (détection de
conflit entre les 3 raccourcis de ce module) sans bookkeeping supplémentaire —
il suffit de vérifier la valeur de retour de `RegisterHotKey`.

Le message `WM_HOTKEY` est enregistré contre la `HudWindow` existante (feature
001), déjà vivante pendant toute la durée de vie de l'application, y compris
masquée (`Hide()` ne détruit pas le handle Win32, seul `Close()` le ferait) —
pas besoin de fenêtre invisible dédiée.

**Alternatives considered**:
- **Hook clavier bas niveau (`WH_KEYBOARD_LL`, approche du projet de
  référence)** : rejeté — plus complexe (boucle de message dédiée, gestion
  manuelle des scan codes AZERTY), et `RegisterHotKey` couvre déjà le besoin
  (raccourcis avec modificateur, pas de capture de touche seule nécessaire ici
  contrairement à Doframe qui liait aussi des touches seules).
- **Bibliothèque tierce de hooking clavier** : rejetée — aucune dépendance
  externe n'est nécessaire pour ce besoin, cohérent avec l'usage minimal de
  dépendances déjà en place.

## Decision: Activation de fenêtre — reprise de la "danse" Alt de Doframe

**Rationale**: `SetForegroundWindow` est bridé par Windows : un processus qui
n'est pas déjà au premier plan peut se voir refuser silencieusement le focus.
Le projet de référence (`logic.py::focus_window`) contourne cette restriction
avec une séquence connue et légitime : restaurer si minimisé
(`IsIconic`/`ShowWindow`), `AllowSetForegroundWindow(pid)`, simuler une frappe
Alt (appui + relâchement via `keybd_event`) juste avant `SetForegroundWindow`
— Windows assouplit la restriction si le dernier évènement d'entrée était une
frappe clavier — puis vérifier par sondage court (jusqu'à ~300 ms) que
`GetForegroundWindow()` correspond bien à la cible.

Cette séquence est reprise à l'identique dans `WindowActivator`
(`Pofus.Platform`), **à la différence près que Doframe avale l'échec
silencieusement (`except: pass`)** — ici, un échec après le délai de sondage
est explicitement journalisé (Principe I) et remonté comme `bool` de retour,
jamais avalé.

**Alternatives considered**:
- **`SetForegroundWindow` seul, sans la danse Alt** : rejeté — échoue
  silencieusement dans un nombre significatif de cas réels (restriction
  Windows), ce qui violerait Principe I si l'échec n'était pas au moins
  détecté et journalisé.

## Decision: Emplacement de la configuration des raccourcis dans le HUD

**Rationale**: Le slot "macros" (déjà prévu dans la disposition par défaut du
HUD, feature 001) accueille ce module — la navigation est la première
capacité d'automatisation livrée, les macros au sens strict (clic synchronisé,
auto-zaap...) viendront s'y ajouter plus tard dans le même esprit modulaire
que "accounts" pour la feature 002. Une fenêtre dédiée
(`NavigationShortcutsWindow`) liste les 3 actions et permet de ré-assigner
chacune, cohérent avec le pattern déjà établi (`AccountManagerWindow`).

**Alternatives considered**:
- **Nouveau slot dédié "navigation"** : rejeté pour l'instant — le slot
  "macros" existe déjà et son rôle correspond exactement à cette
  fonctionnalité ; créer un slot supplémentaire changerait la disposition par
  défaut du HUD sans bénéfice clair.

## Decision: Capture de la combinaison au clavier (UI de configuration)

**Rationale**: Pour ré-assigner un raccourci, l'utilisateur appuie sur la
nouvelle combinaison directement dans l'interface (capture des touches
`PreviewKeyDown`/modificateurs) plutôt que de la taper sous forme de texte —
plus fiable et plus proche de l'expérience standard de reconfiguration de
raccourcis.

**Alternatives considered**:
- **Saisie texte libre ("Ctrl+Tab")** : rejeté — sujet aux fautes de frappe et
  aux formats ambigus ; la capture directe élimine cette classe d'erreurs
  (cohérent avec Principe I).

## Decision: Persistance des raccourcis

**Rationale**: Fichier JSON local dédié
(`%APPDATA%\Pofus\navigation-shortcuts.json`), séparé des autres fichiers de
préférences (feature 001, 002) — même principe d'indépendance par domaine déjà
appliqué. Valeurs par défaut : combinaisons avec modificateur uniquement
(`Ctrl+Tab` / `Ctrl+Maj+Tab` / `Ctrl+L`), contrairement aux touches seules du
projet de référence (`tab`, `²`) qui interféreraient avec la saisie normale ou
les contrôles du jeu (cf. spec.md Assumptions).

**Alternatives considered**: Aucune — cohérent avec le pattern déjà établi
(`HudLayoutStore`, `AccountPreferencesStore`).

## Outstanding NEEDS CLARIFICATION

Aucune.
