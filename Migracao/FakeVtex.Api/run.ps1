Set-Location $PSScriptRoot
pip install -r requirements.txt
uvicorn app:app --reload --port 7001