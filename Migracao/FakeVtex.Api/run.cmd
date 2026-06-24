@echo off
cd /d %~dp0
pip install -r requirements.txt
uvicorn app:app --reload --port 7001