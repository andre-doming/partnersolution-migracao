from __future__ import annotations

from datetime import datetime

from fastapi import APIRouter, Depends

from auth import require_vtex_auth
from models import HealthResponse
from storage import InMemoryStorage


def build_router(storage: InMemoryStorage) -> APIRouter:
    router = APIRouter()

    @router.get("/health", response_model=HealthResponse)
    def health_check(_: None = Depends(require_vtex_auth)) -> HealthResponse:
        uptime = int((datetime.utcnow() - storage.diagnostics.started_at).total_seconds())
        return HealthResponse(
            status="healthy",
            clients=len(storage.clients),
            failureRate=storage.diagnostics.failure_rate,
            uptimeSeconds=uptime,
        )

    return router