# Quest Tool (FastAPI)

Un petit outil web pour enregistrer rapidement des quêtes dans vos tables existantes (NPC, questions/réponses, données de quête, objectifs, étapes, items, monstres).

## Installation

1. Créez un fichier `.env` à la racine en vous basant sur `.env.example` (renseignez `DATABASE_URL`). Par défaut, SQLite local sera utilisé.
2. Installez les dépendances:

```bash
python -m pip install -r requirements.txt
```

## Lancer

```bash
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Ouvrez `http://localhost:8000` dans le navigateur.

## Notes

- Le schéma est découvert dynamiquement via introspection SQLAlchemy. Les champs PK auto-incrément sont détectés et peuvent être laissés vides.
- Si vous fournissez un `quest_id` en haut du formulaire, il sera propagé automatiquement vers les colonnes du même nom (par ex. `quest_id`, `id_quest`, `questId`) lorsque présentes.
- Vous pouvez changer la liste des tables avec `TABLE_NAMES` dans `.env`.
