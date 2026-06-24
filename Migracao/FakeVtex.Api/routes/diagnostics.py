from __future__ import annotations

from fastapi import APIRouter, Depends

from auth import require_vtex_auth
from error_simulator import apply_failure_rate_change, apply_forced_mode
from models import DiagnosticsResponse, FailureModeRequest, FailureRateRequest
from storage import InMemoryStorage


def build_router(storage: InMemoryStorage) -> APIRouter:
    router = APIRouter(tags=["Diagnostics"])

    @router.get("/diagnostics", response_model=DiagnosticsResponse)
    def get_diagnostics(_: None = Depends(require_vtex_auth)) -> DiagnosticsResponse:
        return DiagnosticsResponse(
            totalRequests=storage.diagnostics.total_requests,
            totalClients=len(storage.clients),
            successCount=storage.diagnostics.success_count,
            errorCount=storage.diagnostics.error_count,
            timeoutCount=storage.diagnostics.timeout_count,
            currentFailureRate=storage.diagnostics.failure_rate,
            forcedFailureMode=storage.diagnostics.forced_failure_mode,
        )

    @router.post("/diagnostics/reset")
    def reset_diagnostics(_: None = Depends(require_vtex_auth)) -> dict:
        storage.reset_diagnostics()
        return {"status": "reset"}

    @router.post("/diagnostics/failure-rate")
    def set_failure_rate(payload: FailureRateRequest, _: None = Depends(require_vtex_auth)) -> dict:
        apply_failure_rate_change(storage.diagnostics, payload.failureRate)
        return {"failureRate": storage.diagnostics.failure_rate}

    @router.post("/diagnostics/failure-mode")
    def set_failure_mode(payload: FailureModeRequest, _: None = Depends(require_vtex_auth)) -> dict:
        mode = apply_forced_mode(storage.diagnostics, payload.mode)
        return {"forcedFailureMode": mode}

    return router