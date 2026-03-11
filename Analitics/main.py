from functools import wraps
import time
import sys
import os
import spacy
import requests
import hashlib
import json
import fitz  # PyMuPDF
from docx import Document as DocxReader
WPM = 200  # Parole al minuto per il calcolo del tempo di lettura
# Caricamento del modello di lingua italiana di spaCy
try:
    nlp = spacy.load("it_core_news_sm")
except OSError:
    print("Errore: Modello 'it_core_news_sm' non trovato.")
    sys.exit(1)
    
#Funzione per calcolare l'hash del file
def get_file_hash(file_path):
    hash = hashlib.sha256()
    with open(file_path, "rb") as f:
        for byte_block in iter(lambda: f.read(4096), b""):
            hash.update(byte_block)
    return hash.hexdigest()

#Funzione per estrarre il testo da file di diversi formati
def extract_text(file_path):
    ext = os.path.splitext(file_path)[1].lower()
    testo = ""
    if ext == ".txt":
        with open(file_path, 'r', encoding='utf-8') as f:
            testo = f.read()
    elif ext == ".pdf":
        doc = fitz.open(file_path)
        for pagina in doc:
            testo += pagina.get_text()
        doc.close()
    elif ext == ".docx":
        doc = DocxReader(file_path)
        testo = "\n".join([para.text for para in doc.paragraphs])
    else:
        return 1
    return testo

def measure_time(func):
    @wraps(func)
    def wrapper(*args, **kwargs):
        start_time = time.perf_counter()
        result = func(*args, **kwargs)
        end_time = time.perf_counter()
        #print(f"Tempo di elaborazione per '{func.__name__}': {end_time - start_time:.4f} secondi\n")
        return result + (end_time - start_time,)
    return wrapper

#Funzione per analizzare il testo e calcolare le statistiche richieste
@measure_time
def analitics_text(file_path):
    try:
        testo = extract_text(file_path)
    except Exception as e:
        print(f"Errore estrazione testo: {e}")
        sys.exit(1)
    if testo == 1:
        print("Formato file non supportato per l'estrazione del testo.")
        sys.exit(0)
    viste = set()
    duplicate = set()
    parole=testo.split()
    for  p in parole:
        if p in viste:
            duplicate.add(p)
        else:
            viste.add(p)
    doc = nlp(testo)
    n_lettere = sum(len(token.text) for token in doc if token.is_alpha)
    n_parole = len([token for token in doc if not token.is_punct])
    n_frasi = len(list(doc.sents))
    if n_parole == 0:
        indice = 0
        tempo_lettura_minuti = 0
        uniche = 0
    else:
        # Formula Gulpease: 89 + (300 * frasi - 10 * lettere) / parole
        indice = 89 + (300 * n_frasi - 10 * n_lettere) / n_parole
        tempo_lettura_minuti = n_parole/WPM
        uniche=len(viste)-len(duplicate)
    return indice, n_lettere, n_parole, n_frasi, tempo_lettura_minuti, uniche

def main():
    if len(sys.argv) > 1:
        file_path = sys.argv[1]
        print(f"Python ha ricevuto: {file_path}")
    else:
        print("Nessun parametro ricevuto")
        sys.exit(1)
    try:
        hash = get_file_hash(file_path)
    except Exception as e:
        print(f"Errore calcolo hash: {e}")
        sys.exit(1)
    indice, n_lettere, n_parole, n_frasi, tempo_lettura_minuti, parole_uniche, tempo_elaborazione = analitics_text(file_path)
    # Creazione del json da inviare al server Go
    print(f"Analisi completata: Indice Gulpease: {indice}, Lettere: {n_lettere}, Parole: {n_parole}, Frasi: {n_frasi}, Tempo di lettura (minuti): {tempo_lettura_minuti}, Parole uniche: {parole_uniche}, Tempo di elaborazione: {tempo_elaborazione} secondi")
    data_analitics = {
        "file_path": file_path,
        "gulpease_index": indice,
        "letters": n_lettere,
        "words": n_parole,
        "sentences": n_frasi,
        "read_time": tempo_lettura_minuti,
        "time_analysis": tempo_elaborazione,
        "unique_words": parole_uniche
    }
    data = {
        "hash": hash,
        "analitics": data_analitics
    }
    url = "http://localhost:8080/upload_analitics"
    try:
        response = requests.post(url, json=data)
        if response.status_code != 200:
            print(f"Server Go ha risposto con errore: {response.status_code}")
    except Exception as e:
        print(f"Errore durante l'invio al server Go: {e}")

if __name__ == "__main__":
    main()