from __future__ import annotations

from typing import List

from fastapi import APIRouter, Depends, HTTPException, status

from auth import require_vtex_auth
from error_simulator import maybe_fail
from models import ClientDocument, ClientPayload, ClientSearchResponse, ClientSyncResponse
from storage import InMemoryStorage


def build_router(storage: InMemoryStorage) -> APIRouter:
    router = APIRouter(prefix="/api/dataentities/CL", tags=["Master Data"])

    @router.get("/search", response_model=List[ClientSearchResponse])
    def search_clients(_: None = Depends(require_vtex_auth)) -> List[ClientSearchResponse]:
        storage.diagnostics.total_requests += 1
        maybe_fail(storage.diagnostics)
        storage.diagnostics.success_count += 1
        return [ClientSearchResponse(id=client.id) for client in storage.list_clients()]

    @router.get("/documents/{client_id}", response_model=ClientDocument)
    def get_client(client_id: str, _: None = Depends(require_vtex_auth)) -> ClientDocument:
        storage.diagnostics.total_requests += 1
        maybe_fail(storage.diagnostics)
        client = storage.get_client(client_id)
        if not client:
            storage.diagnostics.error_count += 1
            raise HTTPException(status_code=404, detail="Client not found")
        storage.diagnostics.success_count += 1
        return client

    @router.post("/documents", response_model=ClientSyncResponse, status_code=201)
    def create_client(payload: ClientPayload, _: None = Depends(require_vtex_auth)) -> ClientSyncResponse:
        storage.diagnostics.total_requests += 1
        maybe_fail(storage.diagnostics)
        client = storage.create_client(payload)
        storage.diagnostics.success_count += 1
        return ClientSyncResponse(id=client.id, statusCode=201, success=True)

    @router.put("/documents/{client_id}", response_model=ClientSyncResponse)
    def upsert_client(client_id: str, payload: ClientPayload, _: None = Depends(require_vtex_auth)) -> ClientSyncResponse:
        storage.diagnostics.total_requests += 1
        maybe_fail(storage.diagnostics)
        client = storage.upsert_client(client_id, payload)
        storage.diagnostics.success_count += 1
        return ClientSyncResponse(id=client.id, statusCode=200, success=True)

    @router.delete("/documents/{client_id}", status_code=200)
    def delete_client(client_id: str, _: None = Depends(require_vtex_auth)) -> dict:
        storage.diagnostics.total_requests += 1
        maybe_fail(storage.diagnostics)
        if not storage.delete_client(client_id):
            storage.diagnostics.error_count += 1
            raise HTTPException(status_code=404, detail="Client not found")
        storage.diagnostics.success_count += 1
        return {"deleted": True}

    return router