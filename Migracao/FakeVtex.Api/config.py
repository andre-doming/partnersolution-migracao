from __future__ import annotations

import os
from dataclasses import dataclass

from dotenv import load_dotenv


load_dotenv()


@dataclass(frozen=True)
class Settings:
    failure_rate: int = int(os.getenv("FAILURE_RATE", "1"))
    forced_failure_mode: str = os.getenv("FORCED_FAILURE_MODE", "none")
    log_level: str = os.getenv("LOG_LEVEL", "INFO")
    persist_data: bool = os.getenv("PERSIST_DATA", "false").lower() == "true"
    latency_ms: int = int(os.getenv("LATENCY_MS", "0"))
    rate_limit_per_minute: int = int(os.getenv("RATE_LIMIT_PER_MINUTE", "0"))
    database_url: str = "sqlite:///data/fake_vtex.db"


settings = Settings()
