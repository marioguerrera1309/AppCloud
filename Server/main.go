package main

import (
	"crypto/sha256"
	"database/sql"
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"time" // libreria aggiunta per gestire i timestamp di registrazione dei file

	_ "modernc.org/sqlite"
)

var db *sql.DB

type FileRecord struct {
	Hash      		string 		`json:"hash"`
	Title     		string 		`json:"title"`
	Author    		string 		`json:"author"`
	Date      		string 		`json:"date"`
	SizeBytes 		int64  		`json:"size_bytes"`
	FilePath  		string 		`json:"file_path"`
}

type AuthRequest struct {
	Username 		string 		`json:"username"`
	Password 		string 		`json:"password"`
}

type Analitics struct {
    GulpeaseIndex 	float64    	`json:"gulpease_index"`
	Letters 		int 		`json:"letters"`
	Words   		int 		`json:"words"`
	Sentences 		int 		`json:"sentences"`
	ReadTime		float64		`json:"read_time"`
	TimeAnalysis	float64		`json:"time_analysis"`
	UniqueWords		int			`json:"unique_words"`
}

type Data struct {
	Hash      		string 		`json:"hash"`
	Analitics		Analitics 	`json:"analitics"`
}

func initDatabase() {
	var err error
	// Apre (o crea) il file CloudFG.db. Nota il nome del driver "sqlite"
	db, err = sql.Open("sqlite", "./CloudFG.db")
	if err != nil {
		log.Fatal("Errore apertura DB:", err)
	}
	// Creazione della tabella per i file e i loro metadati
	statement := `
    CREATE TABLE IF NOT EXISTS files (
        hash TEXT,
		author TEXT,
        title TEXT,
		upload_time DATETIME,
        size_bytes INTEGER,
        file_path TEXT,
		PRIMARY KEY (hash, author)
    );`
	_, err = db.Exec(statement)
	if err != nil {
		log.Fatal("Errore creazione tabella:", err)
	}
	// Creazione della tabella per gli utenti
	statement = `
	CREATE TABLE IF NOT EXISTS users (
		username TEXT PRIMARY KEY,
		password TEXT
	);`
	_, err = db.Exec(statement)
	if err != nil {
		log.Fatal("Errore creazione tabella:", err)
	}
	// Creazione della tabella per gli analitics
	statement = `
	CREATE TABLE IF NOT EXISTS analitics (
		hash TEXT PRIMARY KEY,
		gulpease_index REAL,
		letters INTEGER,
		words INTEGER,
		sentences INTEGER,
		read_time REAL,
		time_analysis REAL,
		unique_words INTEGER
	);`
	_, err = db.Exec(statement)
	if err != nil {
		log.Fatal("Errore creazione tabella:", err)
	}
	fmt.Println("Database pronto!")
}
// Handler per la gestione dell'upload dei file
func uploadHandler(w http.ResponseWriter, r *http.Request) {
	//Limita il file a 10MB
	//r.ParseMultipartForm(10 << 20)
	//Recupera il documento dal form con la chiave "file"
	file, handler, err := r.FormFile("file")
	title := r.FormValue("title")
	author := r.FormValue("author")
	if err != nil {
		http.Error(w, "Errore nel recupero del documento", http.StatusBadRequest)
		return
	}
	defer file.Close()
	hasher := sha256.New()
	//fmt.Printf("Ricevuto documento: %s (%d bytes)\n", handler.Filename, handler.Size)
	dstPath := "./uploads"
	//Crea la cartella di destinazione se non esiste
	os.MkdirAll(dstPath, os.ModePerm)
	/* Costruisco il percorso completo del file di destinazione
	"./uploads/nomefile.ext"
	string(os.PathSeparator) gestisce il separatore di percorso in base al sistema operativo
	"/" su Linux e "\" su Windows */
	fullPath := dstPath + string(os.PathSeparator) + handler.Filename
	filename := handler.Filename
	extension := ""
	for i := len(filename) - 1; i >= 0 && !os.IsPathSeparator(filename[i]); i-- {
		if filename[i] == '.' {
			extension = filename[i:]
			filename = filename[:i]
			break
		}
	}
	counter := 1
	for {
		_, err := os.Stat(fullPath)
		if os.IsNotExist(err) {
			// Il file non esiste usciamo dal ciclo
			break
		}
		// Se esiste, generiamo un nuovo nome: nome(1).ext, nome(2).ext, ecc.
		newFilename := fmt.Sprintf("%s(%d)%s", filename, counter, extension)
		fullPath = dstPath + string(os.PathSeparator) + newFilename
		counter++
	}
	//Crea il file nella cartella di destinazione
	dst, err := os.Create(fullPath)
	if err != nil {
		http.Error(w, "Errore nel salvataggio", http.StatusInternalServerError)
		return
	}
	//con defer chiude il file di destinazione alla fine della funzione
	defer dst.Close()
	//Copia il contenuto del file caricato nel file di destinazione a blocchi di 32KB
	var buffer = make([]byte, 32*1024) //Buffer di 32KB
	var written int64
	var i int
	i = 0
	for {
		n, readErr := file.Read(buffer)
		if n > 0 {
			x, writeErr := dst.Write(buffer[:n])
			//fmt.Printf("Letti %d byte, Scritti %d byte, Iterazione %d\n", n, x, i)
			if writeErr != nil || x != n {
				http.Error(w, "Errore in scrittura", 500)
				return
			}
			hasher.Write(buffer[:n])
			written += int64(x)
		}
		if readErr != nil {
			if readErr.Error() == "EOF" {
				break
			}
			http.Error(w, "Errore in lettura", 500)
			return
		}
		i++
	}
	//fmt.Printf("Copiati %d byte\n", written)
	fileHash := fmt.Sprintf("%x", hasher.Sum(nil))
	var exists bool
	err = db.QueryRow("SELECT EXISTS(SELECT 1 FROM files WHERE hash=? AND author=?)", fileHash, author).Scan(&exists)
	if exists {
		fmt.Printf("Duplicato rilevato! Hash: %s\n", fileHash)
		http.Error(w, "File già esistente nel database", http.StatusConflict) // Codice 409
		dst.Close()
		os.Remove(fullPath)
		return
	}
	timestamp := time.Now().Format("2006-01-02 15:04:05")
	_, err = db.Exec("INSERT INTO files (hash, title, author, upload_time, size_bytes, file_path) VALUES (?, ?, ?, ?, ?, ?)",
		fileHash, title, author, timestamp, written, fullPath)
	if err != nil {
		http.Error(w, "Errore registrazione DB", http.StatusInternalServerError)
		return
	}
	fmt.Printf("Salvato: %s | Author: %s | Hash: %s | Bytes: %d | Path: %s \n", title, author, fileHash, written, fullPath)
	w.WriteHeader(http.StatusOK)
	fmt.Fprintf(w, "File caricato con successo!")
	go startPythonAnalysis(fullPath)
	//fmt.Fprintf(w, "File caricato correttamente: %s", handler.Filename)
}
// Funzione per avviare l'analisi del file con lo script Python
func startPythonAnalysis(filePath string) {
	fmt.Printf("Analizzatore python avviato per: %s\n", filePath)
	scriptPath := "../Analitics/main.py"
	cmd := exec.Command("../Analitics/.venv/Scripts/python.exe", scriptPath, filePath)
	//Stampa l'output di Python direttamente sulla console del server Go
	cmd.Stdout = os.Stdout
    cmd.Stderr = os.Stderr
	err := cmd.Run()
	if err != nil {
		fmt.Printf("Errore durante l'esecuzione di Python: %v\n", err)
	}
}
// Handler per la gestione della ricerca dei file in base al titolo e all'autore
func searchHandler(w http.ResponseWriter, r *http.Request) {
	queryText := r.URL.Query().Get("query")
	author := r.URL.Query().Get("user")
	// Esegue la query sul database
	rows, err := db.Query("SELECT hash, title, author, upload_time, size_bytes, file_path FROM files WHERE (title LIKE ?) AND author=?",
		"%"+queryText+"%", author)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer rows.Close()
	var results []FileRecord
	for rows.Next() {
		var f FileRecord
		if err := rows.Scan(&f.Hash, &f.Title, &f.Author, &f.Date, &f.SizeBytes, &f.FilePath); err != nil {
			continue
		}
		f.FilePath = filepath.Base(f.FilePath)
		results = append(results, f)
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(results)
}
// Handler per la visualizzazione di tutti i file di un utente
func searchAllHandler(w http.ResponseWriter, r *http.Request) {
	author := r.URL.Query().Get("user")
	// Esegue la query sul database
	rows, err := db.Query("SELECT hash, title, author, upload_time, size_bytes, file_path FROM files WHERE author=?", author)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer rows.Close()
	var results []FileRecord
	for rows.Next() {
		var f FileRecord
		if err := rows.Scan(&f.Hash, &f.Title, &f.Author, &f.Date, &f.SizeBytes, &f.FilePath); err != nil {
			continue
		}
		f.FilePath = filepath.Base(f.FilePath)
		results = append(results, f)
	}
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(results)
}
// Handler per la gestione del download dei file
func downloadHandler(w http.ResponseWriter, r *http.Request) {
	fileHash := r.URL.Query().Get("hash")
	author := r.URL.Query().Get("user")
	var filePath string
	err := db.QueryRow("SELECT file_path FROM files WHERE hash = ? AND author= ? LIMIT 1", fileHash, author).Scan(&filePath)
	if err != nil {
		http.Error(w, "File non trovato", http.StatusNotFound)
		return
	}
	w.Header().Set("Content-Disposition", "attachment; filename="+filePath)
	http.ServeFile(w, r, filePath)
}
// Funzione per eliminare un file dalla tabella files, la tabella analitics (se non ci sono altri file con lo stesso hash) e il file fisico da /uploads
func Delete(fileHash string, author string) error {
	var filePath string
	fmt.Printf("Elimazione file %s di %s\n", fileHash, author)
	// Si recupera il percorso per cancellare il file anche in memoria
	err := db.QueryRow("SELECT file_path FROM files WHERE hash = ? AND author = ?", fileHash, author).Scan(&filePath)
	if err != nil {
		return err
	}
	//fmt.Printf("Percorso del file da eliminare: %s\n", filePath)
	// Eliminazione da files
	_, err = db.Exec("DELETE FROM files WHERE hash = ? AND author = ?", fileHash, author)
	if err != nil {
		fmt.Printf("Errore eliminazione dal database: %s\n", err)
		return err
	}
	//rowsAffected, _ := x.RowsAffected() 
	//fmt.Printf("File con hash %s eliminato dal database (righe eliminate: %d)\n", fileHash, rowsAffected)
	// Eliminazione da /uploads
	os.Remove(filePath)
	// Eliminazione degli analitics associati al file
	var count int
	err = db.QueryRow("SELECT COUNT(*) FROM files WHERE hash = ?", fileHash).Scan(&count)
	if err == nil && count == 0 {
		// Significa che non ci sono utenti con lo stesso file quindi possiamo eliminare gli analitics
		db.Exec("DELETE FROM analitics WHERE hash = ?", fileHash)
	}
	// Se siamo qui tutto è andato a buon fine e ritorniamo nil (nessun errore)
	return nil
}
// Handler per la gestione dell'eliminazione dei file
func deleteHandler(w http.ResponseWriter, r *http.Request) {
	fileHash := r.URL.Query().Get("hash")
	author := r.URL.Query().Get("user")
	err := Delete(fileHash, author)
	if err != nil {
		http.Error(w, "Errore eliminazione file", http.StatusInternalServerError)
		return
	}
	w.WriteHeader(http.StatusOK)
	fmt.Fprintf(w, "File eliminato con successo")
}

// Prende in input una stringa e restituisce il suo hash SHA256 come stringa esadecimale
func hashString(input string) string {
	h := sha256.New()
	h.Write([]byte(input))
	return fmt.Sprintf("%x", h.Sum(nil))
}
// Handler per la gestione della registrazione degli utenti
func registerHandler(w http.ResponseWriter, r *http.Request) {
	var req AuthRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "JSON non valido", http.StatusBadRequest)
		return
	}
	if req.Username == "" || req.Password == "" {
		http.Error(w, "Username e Password sono obbligatori!", http.StatusBadRequest)
		return
	}
	_, err := db.Exec("INSERT INTO users (username, password) VALUES (?, ?)", req.Username, hashString(req.Password))
	if err != nil {
		http.Error(w, "Username già esistente", http.StatusConflict)
		return
	}
	// Dopo la registrazione viene generato il token di accesso
	token := fmt.Sprintf("%s-%d", req.Username, time.Now().Unix())
	w.WriteHeader(http.StatusCreated)
	fmt.Fprint(w, token)
}
// Handler per la gestione del login degli utenti
func loginHandler(w http.ResponseWriter, r *http.Request) {
	var req AuthRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "JSON non valido", http.StatusBadRequest)
		return
	}
	var storedPassword string
	err := db.QueryRow("SELECT password FROM users WHERE username = ?", req.Username).Scan(&storedPassword)
	if err != nil || storedPassword != hashString(req.Password) {
		http.Error(w, "Credenziali non valide. Username o Password errate", http.StatusUnauthorized)
		return
	}
	// Login riuscito: restituiamo il cookie
	token := fmt.Sprintf("%s-%d", req.Username, time.Now().Unix())
	fmt.Fprint(w, token)
}
// Handler per la gestione dell'eliminazione degli utenti e di tutti i loro file
func deleteUserHandler(w http.ResponseWriter, r *http.Request) {
	author := r.URL.Query().Get("user")
	if author == "" {
		http.Error(w, "Parametro 'user' mancante", http.StatusBadRequest)
		return
	}
	//Conservo gli hash dei file da eliminare in un array per poi eliminarli uno ad uno con la funzione Delete 
	//In questo modo elimina anche gli analitics associati se non ci sono altri file con lo stesso hash
	var fileToDelete []string // array di stringhe per conservare gli hash dei file da eliminare
	rows, err := db.Query("SELECT hash FROM files WHERE author = ?", author)
	if err == nil {
		for rows.Next() {
			// Per ogni file dell'utente prendo l'hash e lo aggiungo all'array fileToDelete
			var h string
			if err := rows.Scan(&h); err == nil { // Scansiona la riga e assegna l'hash alla variabile h
				fileToDelete = append(fileToDelete, h)
			}
    	}
    	rows.Close()
	}
	// Elimino tutti i file usando Delete
	for _, hash := range fileToDelete {
		Delete(hash, author)
	}
	_, err = db.Exec("DELETE FROM users WHERE username = ?", author)
	if err != nil {
		http.Error(w, "Errore eliminazione utente dal DB", http.StatusInternalServerError)
		return
	}
	w.WriteHeader(http.StatusOK)
	fmt.Fprintf(w, "Account e dati di '%s' rimossi correttamente", author)
}
// Handler per la gestione dell'upload degli analitics
func uploadAnaliticsHandler(w http.ResponseWriter, r *http.Request) {
    if r.Method != http.MethodPost {
        http.Error(w, "Metodo non consentito", http.StatusMethodNotAllowed)
        return
    }
	//fmt.Println("Ricevuta richiesta di upload analitics")
	var data Data
	if err := json.NewDecoder(r.Body).Decode(&data); err != nil {
		http.Error(w, "JSON non valido", http.StatusBadRequest)
		return
	}
	//fmt.Printf("Dati ricevuti: Hash: %s, Gulpease: %.2f, Lettere: %d, Parole: %d, Frasi: %d, UniqueWords: %d\n", data.Hash, data.Analitics.GulpeaseIndex, data.Analitics.Letters, data.Analitics.Words, data.Analitics.Sentences, data.Analitics.UniqueWords)
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(map[string]string{"status": "success"})
	// Salvataggio degli analitics nel database
	_, err := db.Exec("INSERT OR REPLACE INTO analitics (hash, gulpease_index, letters, words, sentences, read_time, time_analysis, unique_words) VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
		data.Hash, data.Analitics.GulpeaseIndex, data.Analitics.Letters, data.Analitics.Words, data.Analitics.Sentences, data.Analitics.ReadTime, data.Analitics.TimeAnalysis, data.Analitics.UniqueWords)
	if err != nil {
		http.Error(w, "Errore salvataggio analitics nel DB", http.StatusInternalServerError)
		return
	}
	fmt.Println("Dati salvati correttamente nel database")
}
// Handler per la gestione del download degli analitics
func downloadAnaliticsHandler(w http.ResponseWriter, r *http.Request) {
	hash := r.URL.Query().Get("hash")
	var analitics Analitics
	err := db.QueryRow("SELECT gulpease_index, letters, words, sentences, read_time, time_analysis, unique_words FROM analitics WHERE hash = ?", hash).Scan(&analitics.GulpeaseIndex, &analitics.Letters, &analitics.Words, &analitics.Sentences, &analitics.ReadTime, &analitics.TimeAnalysis, &analitics.UniqueWords)
	if err != nil {
		http.Error(w, "Errore nel recupero analitics", http.StatusInternalServerError)
		return
	}
	//fmt.Printf("Analitics recuperati per hash %s: Gulpease: %.2f, Lettere: %d, Parole: %d, Frasi: %d, ReadTime:%.2f TimeAnalysis:%.2f UniqueWords:%d\n", hash, analitics.GulpeaseIndex, analitics.Letters, analitics.Words, analitics.Sentences, analitics.ReadTime, analitics.TimeAnalysis, analitics.UniqueWords)
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(analitics)
}

func main() {
	initDatabase()
	http.HandleFunc("/upload", uploadHandler)
	http.HandleFunc("/upload_analitics", uploadAnaliticsHandler)
	http.HandleFunc("/search", searchHandler)
	http.HandleFunc("/search_all", searchAllHandler)
	http.HandleFunc("/download", downloadHandler)
	http.HandleFunc("/download_analitics", downloadAnaliticsHandler)
	http.HandleFunc("/delete", deleteHandler)
	http.HandleFunc("/register", registerHandler)
	http.HandleFunc("/login", loginHandler)
	http.HandleFunc("/delete_user", deleteUserHandler)
	fmt.Println("Server Go avviato su http://localhost:8080")
	if err := http.ListenAndServe(":8080", nil); err != nil {
		fmt.Printf("Errore nell'avvio del server: %s\n", err)
	}
}
