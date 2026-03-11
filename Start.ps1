#Configurazione ambiente venv
Write-Host "--- Configurazione modulo Analitics ---" -ForegroundColor Cyan
cd Analitics
if (-not (Test-Path ".venv")) {
    py -3.13 -m venv .venv
}
& .venv\Scripts\activate.ps1
#Installazione dipendenze
pip install spacy
pip install https://github.com/explosion/spacy-models/releases/download/it_core_news_sm-3.8.0/it_core_news_sm-3.8.0-py3-none-any.whl
pip install python-docx pymupdf
cd ..
#Compilazione e avvio Server Go
Write-Host "--- Compilazione e avvio Server Go ---" -ForegroundColor Cyan
cd Server
go build -o server.exe
Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", "./server.exe"
cd ..
#Avvio Client
Write-Host "--- Avvio Client .NET ---" -ForegroundColor Cyan
cd Client
dotnet run .\App.xaml