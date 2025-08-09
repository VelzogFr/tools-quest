from __future__ import annotations

from typing import Dict, List
from sqlalchemy import create_engine, MetaData, Table, inspect
from sqlalchemy.engine import Engine
from sqlalchemy.orm import sessionmaker

from .config import settings

engine: Engine = create_engine(settings.database_url, future=True)
SessionLocal = sessionmaker(bind=engine, future=True, expire_on_commit=False)
metadata = MetaData()


def reflect_tables(table_names: List[str]) -> Dict[str, Table]:
    inspector = inspect(engine)
    available = set(inspector.get_table_names())
    tables: Dict[str, Table] = {}
    for name in table_names:
        if name not in available:
            # Skip silently if a table is not in DB; UI will note missing
            continue
        tables[name] = Table(name, metadata, autoload_with=engine)
    return tables


def get_table_columns(table: Table) -> List[str]:
    return [c.name for c in table.columns]


def is_autoincrement_pk(table: Table, column_name: str) -> bool:
    column = table.columns.get(column_name)
    if column is None:
        return False
    return bool(column.primary_key and (column.autoincrement is True or column.server_default is not None))