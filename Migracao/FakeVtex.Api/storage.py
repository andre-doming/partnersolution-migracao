from __future__ import annotations

import json
import random
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional

from models import ClientDocument, ClientPayload


@dataclass
class Diagnostics:
    total_requests: int = 0
    success_count: int = 0
    error_count: int = 0
    timeout_count: int = 0
    forced_failure_mode: str = "none"
    failure_rate: int = 1
    started_at: datetime = field(default_factory=datetime.utcnow)


class InMemoryStorage:
    def __init__(self) -> None:
        self.clients: Dict[str, ClientDocument] = {}
        self.diagnostics = Diagnostics()

    def seed_clients(self, seed_path: Path) -> None:
        if not seed_path.exists():
            return
        payloads = json.loads(seed_path.read_text(encoding="utf-8"))
        for payload in payloads:
            client = ClientDocument(**payload)
            self.clients[client.id] = client

    def list_clients(self) -> List[ClientDocument]:
        return list(self.clients.values())

    def get_client(self, client_id: str) -> Optional[ClientDocument]:
        return self.clients.get(client_id)

    def upsert_client(self, client_id: str, payload: ClientPayload) -> ClientDocument:
        client = ClientDocument(id=client_id, **payload.model_dump())
        self.clients[client_id] = client
        return client

    def create_client(self, payload: ClientPayload) -> ClientDocument:
        client_id = f"CL{random.randint(100000, 999999)}"
        while client_id in self.clients:
            client_id = f"CL{random.randint(100000, 999999)}"
        return self.upsert_client(client_id, payload)

    def delete_client(self, client_id: str) -> bool:
        return self.clients.pop(client_id, None) is not None

    def reset_diagnostics(self) -> None:
        failure_rate = self.diagnostics.failure_rate
        forced_mode = self.diagnostics.forced_failure_mode
        self.diagnostics = Diagnostics(failure_rate=failure_rate, forced_failure_mode=forced_mode)