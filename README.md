Requisiti:

Scaricare .NET SDK 10.0.102: https://dotnet.microsoft.com/it-it/download

Scaricare Python 3.13.5: https://www.python.org/ftp/python/3.13.5/python-3.13.5-amd64.exe

Scaricare Go 1.25.6: https://go.dev/dl/

___
Compilazione e avvio automatico del progetto

Assicurarsi di essere nella directory ...\CloudFG\ ed eseguire:
```powershell
./Start.ps1
```
___
Alternativamente all' avvio automatico è possibile effettuare la compilazione e avvio manuale del progetto

1. Per compilare ed eseguire il Server:

Entrare nella directory Server ed eseguire i seguenti comandi
```powershell
go build -o server.exe
./server.exe
```
2. Per preparare l'ambiente del modulo Analitics ed installare le dipendenze:

Entrare nella directory Analitics ed eseguire i seguenti comandi
```powershell
py -m venv .venv
.venv\Scripts\activate
pip install spacy
pip install https://github.com/explosion/spacy-models/releases/download/it_core_news_sm-3.8.0/it_core_news_sm-3.8.0-py3-none-any.whl
pip install python-docx pymupdf
```
3. Per compilare ed eseguire il Client:

Entrare nella directory Client ed eseguire i seguenti comandi
```powershell
dotnet run .\App.xaml
```