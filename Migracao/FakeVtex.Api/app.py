from __future__ import annotations

import logging
from datetime import datetime
from pathlib import Path
from time import perf_counter

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from config import settings
from routes.clients import build_router as build_clients_router
from routes.diagnostics import build_router as build_diagnostics_router
from routes.health import build_router as build_health_router
from storage import InMemoryStorage


def create_app() -> FastAPI:
    app = FastAPI(title="Fake VTEX API", version="1.0.0")

    logging.basicConfig(level=getattr(logging, settings.log_level.upper(), logging.INFO))
    logger = logging.getLogger("fake-vtex")

    storage = InMemoryStorage()
    storage.diagnostics.failure_rate = settings.failure_rate
    storage.diagnostics.forced_failure_mode = settings.forced_failure_mode

    seed_path = Path(__file__).parent / "data" / "seed_clients.json"
    storage.seed_clients(seed_path)

    app.include_router(build_health_router(storage))
    app.include_router(build_clients_router(storage))
    app.include_router(build_diagnostics_router(storage))

    @app.middleware("http")
    async def request_logger(request: Request, call_next):
        start = perf_counter()
        request_id = request.headers.get("RequestId") or request.headers.get("X-Request-Id") or "-"
        response = None
        try:
            response = await call_next(request)
            return response
        finally:
            duration_ms = int((perf_counter() - start) * 1000)
            status_code = response.status_code if response else 500
            logger.info(
                "RequestId=%s Method=%s Path=%s StatusCode=%s DurationMs=%s",
                request_id,
                request.method,
                request.url.path,
                status_code,
                duration_ms,
            )

    @app.exception_handler(Exception)
    async def unhandled_exception_handler(request: Request, exc: Exception):
        logger.exception("Unhandled error")
        return JSONResponse(status_code=500, content={"detail": "Internal server error"})

    return app


app = create_app()