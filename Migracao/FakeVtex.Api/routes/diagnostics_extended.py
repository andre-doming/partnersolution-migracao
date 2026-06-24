from __future__ import annotations

import json
from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException
from error_simulator import apply_failure_rate_change, apply_forced_mode
from models import DiagnosticsResponse, FailureModeRequest, FailureRateRequest
from storage import InMemoryStorage


def build_router(storage: InMemoryStorage) -> APIRouter:
    router = APIRouter(tags=["Diagnostics"])

    @router.post("/diagnostics/reset-all")
    def reset_all() -> dict:
        """Reset all metrics, clients, and configurations to initial state."""
        original_rate = storage.diagnostics.failure_rate
        original_mode = storage.diagnostics.forced_failure_mode
        
        storage.clients.clear()
        storage.reset_diagnostics()
        storage.diagnostics.failure_rate = original_rate
        storage.diagnostics.forced_failure_mode = original_mode
        
        # Reload seeds
        from pathlib import Path
        seed_path = Path(__file__).parent.parent / "data" / "seed_clients.json"
        storage.seed_clients(seed_path)
        
        return {
            "success": True,
            "clientsLoaded": len(storage.clients),
            "metricsReset": True,
            "timestamp": datetime.utcnow().isoformat()
        }

    @router.post("/diagnostics/latency")
    def set_latency(request: dict) -> dict:
        """Set artificial latency for all requests (0-30000 ms)."""
        latency_ms = request.get("delayMs", 0)
        if not (0 <= latency_ms <= 30000):
            raise HTTPException(status_code=400, detail="delayMs must be between 0 and 30000")
        
        storage.diagnostics.latency_ms = latency_ms
        return {"latencyMs": latency_ms}

    @router.post("/diagnostics/rate-limit")
    def set_rate_limit(request: dict) -> dict:
        """Set rate limit (requests per minute)."""
        rpm = request.get("requestsPerMinute", 0)
        if rpm < 0:
            raise HTTPException(status_code=400, detail="requestsPerMinute must be >= 0")
        
        storage.diagnostics.rate_limit_per_minute = rpm
        return {"rateLimit": rpm}

    @router.get("/diagnostics/export")
    def export_state() -> dict:
        """Export current state as JSON."""
        return {
            "clients": [
                {
                    "id": c.id,
                    "firstName": c.firstName,
                    "lastName": c.lastName,
                    "email": c.email,
                    "document": c.document,
                    "companyId": c.companyId,
                    "isActive": c.isActive,
                    "clientGuid": c.clientGuid,
                    "syncedAtUtc": c.syncedAtUtc.isoformat(),
                }
                for c in storage.list_clients()
            ],
            "metrics": {
                "totalRequests": storage.diagnostics.total_requests,
                "totalClients": len(storage.clients),
                "successCount": storage.diagnostics.success_count,
                "errorCount": storage.diagnostics.error_count,
                "timeoutCount": storage.diagnostics.timeout_count,
            },
            "config": {
                "failureRate": storage.diagnostics.failure_rate,
                "forcedFailureMode": storage.diagnostics.forced_failure_mode,
                "latencyMs": getattr(storage.diagnostics, 'latency_ms', 0),
                "rateLimitPerMinute": getattr(storage.diagnostics, 'rate_limit_per_minute', 0),
            },
            "exportedAt": datetime.utcnow().isoformat(),
        }

    @router.post("/diagnostics/import")
    def import_state(payload: dict) -> dict:
        """Import previously exported state."""
        try:
            # Clear current state
            storage.clients.clear()
            
            # Load clients
            if "clients" in payload:
                for client_data in payload["clients"]:
                    from models import ClientDocument
                    client = ClientDocument(**client_data)
                    storage.clients[client.id] = client
            
            # Restore config
            if "config" in payload:
                config = payload["config"]
                storage.diagnostics.failure_rate = config.get("failureRate", 1)
                storage.diagnostics.forced_failure_mode = config.get("forcedFailureMode", "none")
                if hasattr(storage.diagnostics, 'latency_ms'):
                    storage.diagnostics.latency_ms = config.get("latencyMs", 0)
                if hasattr(storage.diagnostics, 'rate_limit_per_minute'):
                    storage.diagnostics.rate_limit_per_minute = config.get("rateLimitPerMinute", 0)
            
            return {
                "success": True,
                "clientsLoaded": len(storage.clients),
                "importedAt": datetime.utcnow().isoformat(),
            }
        except Exception as e:
            raise HTTPException(status_code=400, detail=f"Import failed: {str(e)}")

    return router
