from __future__ import annotations

from fastapi import Header, HTTPException, status


def require_vtex_auth(
    x_vtex_api_appkey: str | None = Header(default=None, alias="X-VTEX-API-AppKey"),
    x_vtex_api_apptoken: str | None = Header(default=None, alias="X-VTEX-API-AppToken"),
) -> None:
    if not x_vtex_api_appkey or not x_vtex_api_apptoken:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing VTEX credentials",
        )