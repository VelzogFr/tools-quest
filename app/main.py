from __future__ import annotations

from typing import Dict, Any

from fastapi import FastAPI, Request, Form
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from sqlalchemy import insert

from .config import settings
from .db import SessionLocal, reflect_tables, get_table_columns, is_autoincrement_pk

app = FastAPI(title="Quest Tool", version="0.1.0")

app.mount("/static", StaticFiles(directory="app/static"), name="static")
templates = Jinja2Templates(directory="app/templates")


@app.get("/", response_class=HTMLResponse)
async def index(request: Request) -> HTMLResponse:
    tables = reflect_tables(settings.table_names)
    missing = [name for name in settings.table_names if name not in tables]
    return templates.TemplateResponse(
        "index.html",
        {
            "request": request,
            "tables": tables,
            "missing_tables": missing,
        },
    )


@app.get("/quest/new", response_class=HTMLResponse)
async def new_quest(request: Request) -> HTMLResponse:
    tables = reflect_tables(settings.table_names)
    table_columns: Dict[str, list[str]] = {name: get_table_columns(tbl) for name, tbl in tables.items()}
    return templates.TemplateResponse(
        "quest_form.html",
        {
            "request": request,
            "table_columns": table_columns,
            "tables": tables,
        },
    )


@app.post("/quest/new")
async def create_quest(request: Request) -> RedirectResponse:
    form = await request.form()
    tables = reflect_tables(settings.table_names)

    # Gather values per table: expect form keys like f"{table}.{column}"
    per_table_values: Dict[str, Dict[str, Any]] = {name: {} for name in tables.keys()}

    for key, value in form.items():
        if not isinstance(key, str):
            continue
        if "." not in key:
            continue
        table_name, column_name = key.split(".", 1)
        if table_name in tables and column_name in tables[table_name].columns:
            # Skip empty values
            if value == "" or value is None:
                continue
            per_table_values[table_name][column_name] = value

    # Optional: propagate a top-level quest id if provided (e.g., quest_id)
    propagated_keys = ("quest_id", "id_quest", "questId")
    top_level_quest_id = form.get("quest_id") or form.get("id_quest") or form.get("questId")
    if top_level_quest_id:
        for table_name, values in per_table_values.items():
            for key in propagated_keys:
                if key in tables[table_name].columns and key not in values:
                    values[key] = top_level_quest_id

    # Execute inserts in a single transaction
    with SessionLocal.begin() as session:
        for table_name, values in per_table_values.items():
            table = tables[table_name]
            if not values:
                continue
            # Remove autoincrement PKs if user attempted to set them empty
            for col in list(values.keys()):
                if is_autoincrement_pk(table, col) and values.get(col) in ("", None):
                    values.pop(col, None)
            stmt = insert(table).values(**values)
            session.execute(stmt)

    return RedirectResponse(url="/", status_code=303)