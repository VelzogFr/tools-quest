# QuestTool WPF (.NET Framework)

Application WPF pour créer facilement des quêtes dans vos tables: `npc`, `npc_template`, `npc_question`, `npc_reponse`, `quest_data`, `quest_objectif`, `quest_etape`, `item_template`, `monster_fr`.

## Ouvrir et lancer

1. Ouvrez `QuestTool/QuestTool.sln` dans Visual Studio (Windows).
2. Renseignez la chaîne de connexion dans `QuestTool.App/App.config` (noeud `connectionStrings`, entrée `DefaultConnection`). Choisissez le provider: SQL Server, MySQL ou PostgreSQL (dé-commentez la bonne ligne).
3. Ajustez la liste des tables dans `appSettings:TableNames` si nécessaire.
4. Restaurez les packages NuGet et lancez l'application (F5).

## Fonctionnalités

- UI Material Design (clair) avec champs générés dynamiquement selon le schéma de chaque table.
- Détection des colonnes auto-incrément (identité) pour ne pas les saisir.
- Champ "Quest ID" global. Si une colonne `quest_id`/`id_quest` existe dans une table, la valeur globale peut être propagée.
- Insertion paramétrée via Dapper.

## Remarques

- Les providers sont pré-déclarés dans `<system.data>` de `App.config` pour MySQL et PostgreSQL.
- Si vous utilisez PostgreSQL, vérifiez les droits sur `information_schema`.
- Cet outil ne supprime pas/modifie les lignes existantes; il ajoute des enregistrements.