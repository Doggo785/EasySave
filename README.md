
# EasySave

> Solution de sauvegarde sécurisée et performante développée par ProSoft.

![EasySave Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![Platform](https://img.shields.io/badge/platform-.NET_10.0-blue) ![License](https://img.shields.io/badge/license-ProSoft_Proprietary-red)

##  À propos

**EasySave** est un logiciel de gestion de sauvegardes développé en **C# .NET 10.0**. Il est conçu pour assurer la sécurité des données des clients ProSoft à travers une interface console légère et performante. 

Cette version (1.0) permet la gestion de travaux de sauvegarde complets ou différentiels, avec un suivi précis via des fichiers de logs et un état en temps réel.

### Fonctionnalités principales

* **Gestion des travaux :** Création, modification et suppression jusqu'à 5 travaux de sauvegarde.
* **Types de sauvegarde :** Support des modes **Complet** (Full) et **Différentiel** (Differential).
* **Multi-lingue :** Interface disponible en **Français** et **Anglais**.
* **Journalisation (Logging) :**
    * Historique journalier au format JSON.
    * État d'avancement en temps réel (State Log) au format JSON.
    *path : %appdata%/ProSoft/EasySave*
* **Exécution flexible :**
    * Mode interactif via menu console.
    * Mode ligne de commande (CLI) pour l'automatisation (plages d'IDs, listes).

## Prérequis techniques

Pour compiler et exécuter ce projet, votre environnement doit disposer de :

* **Système d'exploitation :** Windows (recommandé), Linux ou macOS.
* **Framework :** [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
* **IDE (Optionnel) :** Visual Studio 2026 (avec charge de travail .NET Desktop).

## Installation et Démarrage

1.  **Cloner le dépôt :**
    ```bash
    git clone https://github.com/Doggo785/EasySave.git    
    cd EasySave
    ```

2.  **Compiler la solution :**
    ```bash
    dotnet build EasySave.slnx
    ```

3.  **Lancer l'application :**
    ```bash
    cd src/EasySave
    dotnet run
    ```

## Utilisation

### Mode Interactif (Menu)
Au lancement, naviguez dans le menu à l'aide des touches numériques :
1.  **Lister** les travaux existants.
2.  **Créer** un nouveau travail (Nom, Source, Destination, Type).
3.  **Exécuter** un ou plusieurs travaux.
4.  **Supprimer** un travail.
5.  **Changer la langue** de l'interface.

### Mode Ligne de Commande (CLI)
EasySave peut être intégré dans des scripts grâce à ses arguments de démarrage.

* Exécuter le travail n°1 :
    ```bash
    ./EasySave.exe 1
    ```
* Exécuter les travaux 1 à 3 (Plage) :
    ```bash
    ./EasySave.exe 1-3
    ```
* Exécuter les travaux 1 et 3 (Liste) :
    ```bash
    ./EasySave.exe 1;3
    ```
---
Made with ❤️ by ProSoft Team !
*Angélique Porte, Raphael Tolandal, Noah Durtelle, Stéphane Plathey*