# Changelog

Toutes les évolutions notables de Pofus sont consignées ici.

Le format suit [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
et le versionnage suit [SemVer](https://semver.org/lang/fr/).

## [Non publié]

### Ajouté

- Bouton de fermeture sur la barre du HUD, à droite, séparé des modules par un
  filet. Il quitte Pofus, comme « Quitter » dans la zone de notification.
- Atelier : les vignettes d'« Équipements à fabriquer » sont cliquables. Un clic
  marque l'équipement comme fabriqué : contour vert et halo vert autour de la
  vignette, visibles d'un coup d'œil sur tout le panneau. L'état est conservé,
  indépendamment des cases de la liste de courses, qui suivent les ressources
  achetées.

### Modifié

- Tous les textes sont en blanc pur. Les couleurs restent réglables dans
  Apparence ; une couleur choisie par l'utilisateur n'est pas touchée.
- Atelier : les vignettes d'équipement sont remplies du même noir que celui sur
  lequel DofusBook compose ses icônes, et bordées d'un contour blanc d'un pixel
  qui s'éclaire au survol. Le carré de l'icône se fond ainsi dans la vignette au
  lieu d'y former une seconde boîte. Le détourer aurait été pire : ces images
  n'ont pas de couche alpha et sont dessinées pour le noir — le retirer exposait
  leur liseré blanc et les taches de couleur laissées par la compression WebP le
  long de la silhouette.
- L'icône de la zone de notification a un fond transparent : le médaillon est
  détouré au cercle, sans le carré noir qui l'entourait. Le fichier `.ico`
  fournit maintenant huit tailles, chargées à la taille exacte demandée par la
  zone de notification au lieu d'être réduites après coup.

### Corrigé

- **Pofus se fermait brutalement en déplaçant le widget des personnages.**
  Un clic sur une vignette de personnage activait la fenêtre du jeu *et*
  remontait jusqu'au gestionnaire de déplacement, qui demandait alors à Windows
  de déplacer une fenêtre dont le bouton n'était plus enfoncé. Le clic
  s'arrête maintenant sur la vignette, et tout déplacement vérifie l'état réel
  du bouton avant de commencer.
- Une erreur imprévue n'arrête plus Pofus : elle est journalisée dans
  `%APPDATA%\Pofus\logs\pofus.log` et signalée une fois. Les crashs
  précédents ne laissaient aucune trace dans le journal.
- Fermer le HUD ou le widget des personnages coupait la surveillance du premier
  plan pour *toutes* les fenêtres Pofus, qui repassaient alors derrière le jeu.
- Le halo du chef de groupe ne respirait pas : son animation repartait de zéro
  toutes les deux secondes, à chaque rafraîchissement de la liste. Les vignettes
  sont désormais mises à jour sur place au lieu d'être reconstruites, ce qui
  évite aussi que le widget change de taille sous le curseur pendant un
  déplacement.
- Une fenêtre enregistrée sur un écran débranché depuis revenait hors du bureau,
  donc impossible à rattraper à la souris. Sa position est ramenée dans le
  bureau au démarrage.
- Les fichiers de réglages sont écrits puis remplacés d'un bloc. Une
  interruption pendant l'écriture laissait un fichier tronqué et perdait les
  réglages concernés.
- Les titres des fenêtres (« Atelier DofusBook », « Réglages », « Raccourcis »…)
  s'affichaient dans le noir par défaut de WPF, donc illisibles sur un panneau
  sombre : leur style remplaçait celui du texte courant au lieu d'en hériter.
- Le bouton « Voir les équipements » gardait l'habillage Windows d'origine, un
  bloc gris-blanc au milieu du panneau : les boutons à deux états n'avaient pas
  de style Pofus.
- Une fenêtre fermée par sa croix (Atelier, Comptes, Raccourcis, Réglages) ne
  se rouvrait plus jusqu'au redémarrage de Pofus : `IsLoaded` reste vrai après
  la fermeture en WPF, et le bouton du HUD réactivait donc une fenêtre morte.

## [1.1.0] — 2026-08-20

### Ajouté

- Atelier : bouton « Actualiser » pour relire l'atelier sans recoller le lien,
  et bouton « Voir les équipements » qui déplie un panneau latéral montrant
  les équipements à fabriquer en grandes vignettes.
- Les boutons supplémentaires de la souris (molette, Souris 4, Souris 5)
  peuvent servir de raccourci, avec modificateurs. Un bouton lié à Pofus est
  absorbé et n'agit donc pas aussi dans le jeu.

### Modifié

- Icône propre à l'application, dans l'exécutable, l'installeur et la zone de
  notification.
- Typographie passée à Segoe UI Variable, la police du langage visuel de
  Windows 11, et ombres portées adoucies.
- Poignée de déplacement sur le HUD et le widget des personnages : ils se
  saisissaient jusqu'ici par les quelques pixels laissés libres entre les
  boutons.
- Le lancement au démarrage de Windows place désormais un raccourci dans le
  dossier Démarrage (`shell:startup`) au lieu d'une valeur de registre :
  visible, et supprimable à la main.

### Corrigé

- La désinstallation ne supprime plus les réglages et la liste de craft sans
  demander confirmation.

## [1.0.0] — 2026-08-20

Première version publiée. Pofus est un assistant multi-comptes pour Dofus :
un HUD qui reste au-dessus du jeu, la gestion des personnages connectés, la
navigation au clavier entre fenêtres, et une liste de courses de craft.

### HUD modulaire

- Barre unique flottant au-dessus de **toutes** les fenêtres Dofus à la fois,
  déplaçable, dont la position est conservée d'une session à l'autre
- Architecture à modules : chaque emplacement accueille un module indépendant,
  et le plantage d'un module dégrade son emplacement sans emporter le HUD
- Icône dans la zone de notification pour tout afficher/masquer et quitter

### Gestion des comptes

- Détection automatique des personnages connectés à partir des fenêtres du jeu
  (pseudo et classe), rafraîchie en continu
- Activation/désactivation par personnage, équipes, désignation d'un leader,
  et réordonnancement manuel — le tout persisté
- Widget détachable listant les personnages : icône réelle de la barre des
  tâches (donc l'icône de classe), liseré blanc sur celui qui a le focus,
  et halo blanc irisé « respirant » autour du leader

### Navigation entre fenêtres

- Raccourcis clavier globaux : personnage suivant, précédent, aller au leader
- Fonctionnent quelle que soit la fenêtre active, y compris en jeu
- Réassignables, avec détection des conflits ; les raccourcis sont suspendus
  pendant la saisie d'une nouvelle combinaison, sans quoi une combinaison déjà
  prise resterait invisible à l'écran de configuration

### Réglages

- Détection au démarrage des logiciels concurrents connus, avec possibilité de
  fermer le logiciel détecté, de continuer, ou de ne plus être averti
- Lancement automatique de Pofus au démarrage de Windows
- Apparence personnalisable : couleur et opacité du fond, couleur du texte,
  couleur et opacité des bordures, appliquées en direct à toutes les fenêtres,
  avec préréglages (Sombre, Verre translucide, Contrasté) et réinitialisation.
  Un plancher d'opacité empêche de rendre l'interface introuvable.

### Masquage des fenêtres

- Chaque fenêtre de Pofus se masque d'un clic droit et se réaffiche par un
  raccourci clavier global qui lui est propre
- État et raccourcis persistés ; la zone de notification reste le point
  d'entrée garanti, même sans aucun raccourci configuré

### Atelier (craft)

- Import d'un atelier DofusBook en collant simplement son lien
- Liste de courses agrégée : les quantités d'une même ressource utilisée par
  plusieurs équipements sont additionnées, en s'appuyant sur l'identifiant de
  l'objet et non sur son nom
- Icône de chaque ressource, clic sur le nom pour le copier dans le
  presse-papiers, case à cocher par ressource et compteur de progression
- Fenêtre redimensionnable et maintenue au premier plan

### Notes techniques

- Windows 10/11, aucun prérequis : l'application est distribuée avec son
  propre runtime .NET
- La seule sortie réseau est la lecture de l'atelier DofusBook, déclenchée
  uniquement quand vous cliquez « Importer ». Aucune donnée personnelle n'est
  transmise ; toute la configuration reste dans `%APPDATA%\Pofus`.
- L'import passe par le composant WebView2 de Windows, présent par défaut sur
  Windows 11

[1.1.0]: https://github.com/Ver-Solitaire/Pofus/releases/tag/v1.1.0
[1.0.0]: https://github.com/Ver-Solitaire/Pofus/releases/tag/v1.0.0
