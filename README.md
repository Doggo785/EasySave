
# EasySave

> Solution de sauvegarde sécurisée et performante développée par ProSoft.

![EasySave Build Status](https://img.shields.io/badge/build-passing-brightgreen) ![Version](https://img.shields.io/badge/version-3.0-orange) ![Platform](https://img.shields.io/badge/platform-.NET_10.0-blue) ![License](https://img.shields.io/badge/license-ProSoft_Proprietary-red)

## À propos

**EasySave** est un logiciel de gestion de sauvegardes développé en **C# .NET 10.0**. Il est conçu pour assurer la sécurité des données des clients ProSoft à travers une interface graphique moderne, légère et performante.

Cette version (3.0) introduit une interface graphique complète (GUI) avec **Avalonia UI**, le chiffrement des fichiers, l'exécution parallèle des travaux, un serveur de logs distant et de nombreuses options de personnalisation avancées.

### Fonctionnalités principales

* **Interface graphique (GUI) :** Application de bureau multi-plateforme développée avec **Avalonia UI** et le pattern **MVVM** (ReactiveUI).
* **Thème :** Basculement entre le mode **Sombre** et **Clair** directement depuis les paramètres.
* **Gestion des travaux :** Création, exécution et suppression de travaux de sauvegarde sans limite de nombre.
* **Types de sauvegarde :** Support des modes **Complet** (Full) et **Différentiel** (Differential).
* **Contrôle en temps réel :** **Pause**, **Reprise** et **Annulation** de chaque travail individuellement avec suivi de la progression.
* **Exécution parallèle :** Lancement simultané de plusieurs travaux, avec un nombre de jobs concurrents configurable et une gestion dédiée des **gros fichiers** (seuil en Ko paramétrable).
* **Chiffrement AES-256 :** Chiffrement des fichiers sauvegardés selon une liste d'**extensions configurables**, avec dérivation de clé sécurisée (PBKDF2 / SHA-256).
* **Extensions prioritaires :** Définition d'extensions de fichiers à traiter en priorité avant les autres lors d'une sauvegarde parallèle.
* **Logiciel métier :** Détection automatique de processus bloquants — la sauvegarde est suspendue tant que le logiciel métier défini est en cours d'exécution.
* **Multi-lingue :** Interface disponible en **Français** et **Anglais**.
* **Journalisation (Logging) :**
    * Historique journalier au format **JSON** ou **XML**.
    * État d'avancement en temps réel (State Log).
    * Cible de log configurable : **fichier local**, **serveur distant**, ou **les deux**.
    * *Path : `%appdata%/ProSoft/EasySave`*
* **Serveur de logs (EasyLog.LogServer) :** Application serveur TCP dédiée à la réception et à la centralisation des logs en temps réel, avec indicateur de connexion dans le tableau de bord.

## Architecture de la solution

```
EasySave.slnx
├── src/EasySave.UI         # Interface graphique Avalonia (MVVM / ReactiveUI)
├── src/EasySave.Core       # Logique métier (SaveManager, CryptoService, SettingsManager…)
├── src/EasyLog             # Bibliothèque de journalisation (JSON/XML, TCP)
├── src/EasyLog.LogServer   # Serveur de logs TCP standalone
└── EasySave.Test           # Tests unitaires
```

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
    cd src/EasySave.UI
    dotnet run
    ```

4.  *(Optionnel)* **Lancer le serveur de logs :**
    ```bash
    cd src/EasyLog.LogServer
    dotnet run
    ```

## Utilisation

### Tableau de bord (Home)
À l'ouverture, le tableau de bord affiche un résumé de l'état de l'application :
* Nombre de travaux configurés.
* Heure de la dernière sauvegarde.
* État du processus métier (actif / inactif).
* Format de log actif et statut de connexion au serveur de logs.

### Gestion des travaux (Jobs)
Depuis la vue **Jobs**, vous pouvez :
* **Créer** un nouveau travail (Nom, Source, Destination, Type).
* **Lancer / Mettre en pause / Reprendre / Annuler** chaque travail individuellement.
* Suivre la **progression** de chaque sauvegarde en temps réel.

### Paramètres (Settings)
La vue **Paramètres** centralise toute la configuration :

| Paramètre | Description |
|---|---|
| Thème | Bascule Dark / Light |
| Langue | Français / English |
| Format de log | JSON ou XML |
| Cible de log | Fichier, Serveur, ou Les deux |
| Adresse du serveur | IP et port du serveur EasyLog |
| Logiciels métier | Liste des processus bloquants |
| Extensions chiffrées | Extensions de fichiers à chiffrer (AES-256) |
| Extensions prioritaires | Extensions traitées en priorité |
| Jobs simultanés | Nombre max de travaux en parallèle |
| Seuil gros fichiers | Taille (Ko) à partir de laquelle un fichier est traité séquentiellement |

---
Made with ❤️ by ProSoft Team !
*Angélique Porte, Raphael Tolandal, Noah Durtelle, Stéphane Plathey*