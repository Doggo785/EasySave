# Documentation Technique - EasySave

Cette documentation couvre l'architecture et l'implémentation du logiciel de sauvegarde EasySave.

## Architecture Modulaire

La solution est segmentée en plusieurs composants distincts pour garantir la séparation des responsabilités :

* **EasySave.Core** : Moteur principal gérant les travaux de sauvegarde (SaveJob), la cryptographie (CryptoService) et la vérification des processus (ProcessChecker).
* **EasySave.UI** : Interface graphique développée avec Avalonia UI, implémentant le pattern MVVM.
* **EasySave.Console** : Interface en ligne de commande alternative.
* **EasyLog** : Service de journalisation centralisé gérant les logs journaliers et les logs d'état.

## Navigation Rapide

* [Guides et Architecture (Use Cases, Diagrammes)](docs/introduction.md)
* [Documentation de l'API (Classes et Méthodes)](api/)