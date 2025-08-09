import os
from dotenv import load_dotenv

load_dotenv()

class Settings:
    def __init__(self) -> None:
        self.database_url: str = os.getenv(
            "DATABASE_URL",
            # Default to local SQLite file for quick start
            "sqlite+pysqlite:///./quest_tool.db",
        )
        # Comma-separated list of tables to reflect; override if your names differ
        default_tables = (
            "npc,npc_template,npc_question,npc_reponse,"
            "quest_data,quest_objectif,quest_etape,item_template,monster_fr"
        )
        self.table_names: list[str] = [
            t.strip() for t in os.getenv("TABLE_NAMES", default_tables).split(",") if t.strip()
        ]

settings = Settings()