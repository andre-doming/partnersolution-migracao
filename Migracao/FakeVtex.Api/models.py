from __future__ import annotations

from datetime import datetime
from typing import Optional

from pydantic import BaseModel, Field


class ClientPayload(BaseModel):
    firstName: str = Field(..., min_length=1)
    lastName: str = Field(..., min_length=1)
    email: str = Field(..., min_length=1)
    document: str = Field(..., min_length=1)
    companyId: str = Field(..., min_length=1)
    isActive: bool = True
    clientGuid: str = Field(..., min_length=1)
    syncedAtUtc: datetime = Field(default_factory=datetime.utcnow)


class ClientDocument(ClientPayload):
    id: str


class ClientSearchResponse(BaseModel):
    id: str


class ClientSyncResponse(BaseModel):
    id: Optional[str] = None
    statusCode: Optional[int] = None
    message: Optional[str] = None
    success: bool = True


class HealthResponse(BaseModel):
    status: str
    clients: int
    failureRate: int
    uptimeSeconds: int


class DiagnosticsResponse(BaseModel):
    totalRequests: int
    totalClients: int
    successCount: int
    errorCount: int
    timeoutCount: int
    currentFailureRate: int
    forcedFailureMode: str


class FailureModeRequest(BaseModel):
    mode: str


class FailureRateRequest(BaseModel):
    failureRate: int
