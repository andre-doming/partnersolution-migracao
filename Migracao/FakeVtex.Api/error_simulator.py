from __future__ import annotations

import random
import time

from fastapi import HTTPException, status

from storage import Diagnostics


FAILURE_STATUSES = [
    status.HTTP_429_TOO_MANY_REQUESTS,
    status.HTTP_500_INTERNAL_SERVER_ERROR,
    status.HTTP_502_BAD_GATEWAY,
    status.HTTP_503_SERVICE_UNAVAILABLE,
]


def maybe_fail(diagnostics: Diagnostics) -> None:
    mode = diagnostics.forced_failure_mode
    if mode and mode != "none":
        _apply_failure_mode(mode, diagnostics)
        return

    if diagnostics.failure_rate <= 0:
        return

    threshold = max(1, diagnostics.failure_rate)
    if random.randint(1, 100) <= threshold:
        status_code = random.choice(FAILURE_STATUSES + ["timeout"])
        _apply_failure_mode(status_code, diagnostics)


def _apply_failure_mode(mode: str | int, diagnostics: Diagnostics) -> None:
    if mode == "timeout":
        diagnostics.timeout_count += 1
        time.sleep(2.5)
        raise HTTPException(status_code=status.HTTP_504_GATEWAY_TIMEOUT, detail="Simulated timeout")

    try:
        status_code = int(mode)
    except (TypeError, ValueError):
        status_code = status.HTTP_500_INTERNAL_SERVER_ERROR

    diagnostics.error_count += 1
    raise HTTPException(status_code=status_code, detail="Simulated failure")


def apply_failure_rate_change(diagnostics: Diagnostics, failure_rate: int) -> None:
    diagnostics.failure_rate = max(0, min(100, failure_rate))


def apply_forced_mode(diagnostics: Diagnostics, mode: str) -> str:
    normalized = mode.lower()
    allowed = {"none", "429", "500", "502", "503", "timeout"}
    if normalized not in allowed:
        raise HTTPException(status_code=400, detail="Invalid failure mode")
    diagnostics.forced_failure_mode = normalized
    return normalized