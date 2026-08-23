# Specificație funcțională și tehnică — Aplicația „NT to SAGA”

**Scop:** transferul semi-automat al notelor de transfer între gestiuni dintr-un sistem de gestiune (raport CSV) în programul de contabilitate SAGA (import DBF).

**Versiune document:** 1.1 · **Stare:** specificație validată, pregătită pentru dezvoltare
**Bază:** `Scopul aplicatiei.txt` + fișier CSV exemplu + fișier DBF exemplu + deciziile beneficiarului din 23.08.2026

> Toate cele 11 puncte deschise din versiunea 1.0 au fost soluționate. Deciziile luate sunt marcate cu ✔ **DECIZIE** în text și centralizate în §11.

---

## 1. Context și obiectiv

### 1.1 Situația actuală

Operatorul extrage din sistemul de gestiune raportul *„Centralizatoare intrări și ieșiri nefiscale, inclusiv cele generate automat”* în format CSV. Datele privind notele de transfer între gestiuni trebuie apoi introduse în SAGA. Operațiunea manuală este repetitivă și predispusă la erori.

### 1.2 Ce face aplicația

1. **Importă** liniile din CSV într-o bază de date locală SQLite.
2. **Permite selecția** unui interval de date și a uneia sau mai multor gestiuni sursă.
3. **Generează** fișierul DBF în formatul acceptat de importul SAGA.
4. **Ține evidența** exporturilor efectuate, astfel încât aceleași date să nu fie exportate de două ori din greșeală.

### 1.3 Principiul contabil implementat

O notă de transfer mută marfă de la gestiunea **Name** (sursă) la gestiunea **PartnerName** (destinație). În SAGA aceasta se înregistrează ca **două linii**:

- o linie de **ieșire** din gestiunea sursă — cantitate și valori **negative**;
- o linie de **intrare** în gestiunea destinație — cantitate și valori **pozitive**.

Fiecare linie poartă contul de marfă al gestiunii respective și codul de activitate corespunzător.

### 1.4 Utilizatorul țintă

Personal din departamentul economic, **fără pregătire IT**. Consecințe obligatorii pentru proiectare:

- fără linie de comandă, fără fișiere de configurare editate manual;
- mesaje de eroare în limba română, care spun **ce s-a întâmplat** și **ce trebuie făcut**;
- nicio operațiune distructivă fără confirmare;
- valorile de configurare (denumiri articole, conturi, activități) se modifică din interfață, nu în cod.

---

## 2. Constrângeri impuse

| # | Constrângere | Sursa |
|---|---|---|
| C1 | Aplicația **nu** se dezvoltă în Python | cerință explicită |
| C2 | Aplicația are **interfață grafică** (desktop) | cerință explicită |
| C3 | Folderul aplicației: `d:\nttosaga` | cerință explicită |
| C4 | Fișierele de lucru (CSV intrare / DBF ieșire): `d:\nttosaga\files` | cerință explicită |
| C5 | Bază de date **SQLite** | cerință explicită |
| C6 | Denumirile articolelor pe cotă de TVA **nu se hardcodează** — sunt valori implicite editabile din interfață | cerință explicită |
| C7 | Aplicație non-comercială, utilizator non-tehnic | cerință explicită |

---

## 3. Fișierul de intrare (CSV)

### 3.1 Structura reală a fișierului exemplu

| Caracteristică | Valoare constatată |
|---|---|
| Codificare | UTF-8 **cu BOM** |
| Terminator linie | CRLF |
| Separator | virgulă `,` |
| Număr total coloane | 69 |
| Rândurile 1–3 | antet de raport (`Textbox40`, `Colectia: CUMULATIVA ANCA`, rând gol) — **de ignorat** |
| Rândul 4 | capul de tabel cu numele coloanelor |
| Rândurile 5+ | date |
| Rânduri finale | 3 rânduri complet goale — **de ignorat** |
| Linii de date utile în exemplu | 37 |

Fișierul este un export dintr-un generator de rapoarte, motiv pentru care conține coloane duplicate (`Name` / `Name2`, `DocNumber` / `DocNumber2` — valori identice) și coloane de subtotal intercalate (`Total NT/Nr.4244 iesire:`, `Total 03.08.2026 iesire:`). **Toate acestea se ignoră.**

> **Regulă de implementare:** identificarea coloanelor se face **după numele din capul de tabel**, nu după poziție. Poziția coloanelor se poate schimba la o reconfigurare a raportului sursă.

### 3.2 Coloane preluate

| Coloană CSV | Tip stocare | Observații |
|---|---|---|
| `Name` | Character 25 | gestiunea sursă; în exemplu max. 9 caractere |
| `PartnerName` | Character 25 | gestiunea destinație; în exemplu max. 13 caractere (`DEPOZIT ANCA1`) |
| `DocNumber` | Character 12 | în CSV apare ca număr (`4244`) — se citește **ca text**, fără conversie numerică |
| `DocDate` | Date | format sursă `dd.mm.yyyy` |
| `RetailVatPercent` | Numeric 2 | valori întâlnite: `11`, `21`; se acceptă și `0` |
| `ValAmIesire` | Numeric 15,2 | valoare de vânzare (cu TVA) |
| `ValVatAmIesire` | Numeric 15,2 | TVA aferent |
| `ValAchizitieFaraTVAIesire` | Numeric 15,2 | valoare de achiziție fără TVA |

Coloanele `ValAm`, `ValVatAm`, `ValAchizitieFaraTVA` (intrări) și toate coloanele de total **nu se stochează**.

Coloana `DocumentCode` **nu se folosește și nu se stochează** — consecință a deciziei D1.

### 3.3 Cheia de unicitate

```
Name + DocNumber + DocDate + RetailVatPercent
```

O notă de transfer cu două cote de TVA generează **două înregistrări** distincte (ex.: NT 4244 din 03.08.2026 apare cu 11% și cu 21%). Verificat pe fișierul exemplu: **zero duplicate** pe această cheie.

### 3.4 Reguli de filtrare la import

✔ **DECIZIE D1 — nu se filtrează după tipul documentului.** Toate liniile din CSV se importă, indiferent de conținutul coloanei `DocumentCode`. Nu se implementează filtru pe prefix `NT`.

✔ **DECIZIE D2 — liniile cu `ValAmIesire = 0` se ignoră la import** și se raportează în sumarul importului, la o categorie distinctă. Situația nu este așteptată în practică, dar tratarea ei previne generarea de linii DBF fără conținut economic.

> Notă: literalul `NT` din numele fișierului exportat rămâne o **constantă**, nu rezultatul unui filtru.

---

## 4. Fișierul de ieșire (DBF)

### 4.1 Structura fișierului

Antet dBASE III (versiune `0x03`, fără memo), lungime antet **673** octeți, lungime înregistrare **310** octeți, terminator fișier `0x1A`.

| # | Câmp | Tip | Lung. | Zec. | Conținut |
|---|---|---|---|---|---|
| 1 | `NR_NIR` | **C** | 16 | 0 | necompletat (spații) |
| 2 | `NR_INTRARE` | C | 16 | 0 | `DocNumber` |
| 3 | `GESTIUNE` | C | 4 | 0 | necompletat |
| 4 | **`DEN_GEST`** | C | 36 | 0 | necompletat |
| 5 | `COD` | C | 5 | 0 | constant `20100` |
| 6 | `DATA` | D | 8 | 0 | `DocDate`, format `YYYYMMDD` |
| 7 | `SCADENT` | D | 8 | 0 | **egal cu `DATA`** (D4) |
| 8 | `TIP` | C | 1 | 0 | constant `A` (aviz) |
| 9 | `TVAI` | N | 1 | 0 | **`0`** (D5) |
| 10 | `COD_ART` | C | 16 | 0 | necompletat |
| 11 | `DEN_ART` | C | 60 | 0 | după cota de TVA — §4.3 |
| 12 | `UM` | C | 5 | 0 | `BUC` (majuscule) |
| 13 | `CANTITATE` | N | 14 | 3 | `-1.000` / `1.000` |
| 14 | `DEN_TIP` | C | 36 | 0 | necompletat |
| 15 | `TVA_ART` | N | 2 | 0 | `RetailVatPercent` |
| 16 | `VALOARE` | N | 15 | 2 | ∓`ValAchizitieFaraTVAIesire` |
| 17 | `TVA` | N | 15 | 2 | ∓`ValVatAmIesire` |
| 18 | `CONT` | C | 20 | 0 | §4.4 |
| 19 | `PRET_VANZ` | N | 15 | 2 | `ValAmIesire` — **pozitiv pe ambele linii** |
| 20 | `GRUPA` | C | 16 | 0 | codul de activitate al gestiunii de pe linie |

### 4.2 Divergențe față de textul specificației inițiale — soluționate

Fișierul DBF real este cel citit efectiv de SAGA, deci **primează structura lui**.

| Element | Text specificație | Fișier real | Decizie |
|---|---|---|---|
| Nume câmp 4 | `DENGEST` | `DEN_GEST` | ✔ `DEN_GEST` |
| Tip `NR_NIR` | Numeric 16 | Character 16 | ✔ **Character** |
| `SCADENT` | necompletat | = `DATA` | ✔ **D4: `SCADENT` = `DATA`** |
| `TVAI` | necompletat | `0` | ✔ **D5: `TVAI` = `0`** |
| `UM` | `"buc"` | `BUC` | ✔ `BUC` |
| `DEN_ART` cota 21 | `Parafarmeceutice TVA 21%` | `Parafarmaceutice TVA<>11%` | ✔ **D6: `Parafarmaceutice TVA<>11%`** |

### 4.3 Denumirile articolelor pe cotă de TVA

✔ **DECIZIE D6.** Valori implicite la prima pornire, **editabile din interfață** (constrângerea C6):

| Cota TVA | `DEN_ART` |
|---|---|
| 0 | `Scutite de TVA` |
| 11 | `Medicamente TVA 11%` |
| 21 | `Parafarmaceutice TVA<>11%` |

Atenție la scriere: ortografia validată este *Parafar**ma**ceutice*, iar sufixul este `TVA<>11%` (nu `TVA 21%`). Denumirea trebuie să corespundă exact articolului din nomenclatorul SAGA; o singură diferență de caracter duce la eșecul importului sau la crearea unui articol duplicat. Din acest motiv, câmpul se copiază literal din tabelul de configurare, fără nicio transformare automată (fără schimbare de majuscule, fără eliminarea spațiilor duble).

Tabelul de cote este **extensibil** din interfață: la o modificare viitoare a cotelor de TVA, utilizatorul adaugă o linie nouă fără intervenția unui programator.

### 4.4 Nomenclatorul gestiunilor

| Name / PartnerName | ContMarfa | Activitate (`GRUPA`) |
|---|---|---|
| `ANCAFARM1` | 371.00001 | 01 |
| `ANCAFARM2` | 371.00002 | 02 |
| `ANCAFARM3` | 371.00003 | 03 |
| `ANCAFARM4` | 371.00004 | 04 |
| `DEPOZIT ANCA1` | 371.00006 | 06 |
| `DEPOZIT` | 371.00009 | 09 |
| `LABORATOR` | 371.00008 | 08 |

**Construcția contului:** `CONT = ContMarfa + "." + RetailVatPercent`

✔ **DECIZIE D7 — cota se scrie fără zerouri de completare**, exact ca număr:

| Cota | Rezultat pentru ANCAFARM1 |
|---|---|
| 0 | `371.00001.0` |
| 11 | `371.00001.11` |
| 21 | `371.00001.21` |

Reguli obligatorii de potrivire:

- potrivirea denumirii se face pe **șirul complet**, nu pe prefix. `DEPOZIT ANCA1` și `DEPOZIT` sunt două gestiuni diferite, iar a doua este prefixul primei — o potrivire „începe cu” ar direcționa greșit contul;
- potrivirea este **insensibilă la majuscule** și ignoră spațiile de la început și sfârșit;
- tabelul este **editabil din interfață**.

### 4.5 Generarea celor două linii

Pentru **fiecare** înregistrare importată se scriu **două** înregistrări DBF:

| Câmp | Linia 1 (ieșire din sursă) | Linia 2 (intrare în destinație) |
|---|---|---|
| `CANTITATE` | `-1.000` | `1.000` |
| `VALOARE` | `-ValAchizitieFaraTVAIesire` | `+ValAchizitieFaraTVAIesire` |
| `TVA` | `-ValVatAmIesire` | `+ValVatAmIesire` |
| `PRET_VANZ` | `+ValAmIesire` | `+ValAmIesire` |
| `CONT` | contul gestiunii din `Name` | contul gestiunii din `PartnerName` |
| `GRUPA` | activitatea gestiunii din `Name` | activitatea gestiunii din `PartnerName` |
| restul câmpurilor | identice | identice |

Linia negativă se scrie **înaintea** celei pozitive, iar perechea rămâne întotdeauna împreună.

✔ **DECIZIE D8 — ordinea de sortare a înregistrărilor în fișier:**

```
1. DocDate (crescător)
2. DocNumber
3. RetailVatPercent (crescător)
```

`DocNumber` este stocat ca text, dar reprezintă un număr de document. Sortarea alfabetică ar produce ordinea `1910, 2984, 4244` corect doar cât timp toate au aceeași lungime; la trecerea de la 4 la 5 cifre (`9999` → `10000`) ordinea alfabetică ar plasa `10000` înaintea lui `9999`. **Sortarea se face numeric atunci când `DocNumber` este integral cifre**, altfel alfabetic — cu aceeași regulă aplicată consecvent.

### 4.6 Reguli de scriere a fișierului DBF

Detalii care determină dacă SAGA acceptă sau respinge fișierul:

- **Antet:** octet 0 = `0x03`; octeții 1–3 = data creării (în exemplu `26 08 23`, adică anul pe două cifre — se replică acest comportament); octeții 4–7 = număr înregistrări (uint32 little-endian); 8–9 = lungime antet (`673`); 10–11 = lungime înregistrare (`310`); restul zero, inclusiv octetul 29 (language driver) = `0x00`.
- **Descriptori câmp:** 32 octeți fiecare, nume completat cu `0x00` până la 11 octeți, terminați de `0x0D` după ultimul descriptor.
- **Înregistrări:** primul octet = marcaj ștergere = spațiu (` `).
- **Câmpuri C:** aliniate la stânga, completate cu spații.
- **Câmpuri N:** aliniate la **dreapta**, completate cu spații, separator zecimal `.`, semnul minus lipit de prima cifră. Exemplu real pe 15 poziții: `` `        -542.37` ``.
- **Câmpuri D:** exact 8 caractere `YYYYMMDD`.
- **Final fișier:** octetul `0x1A`.
- **Codificare text:** ASCII. **Diacriticele se elimină** la scriere (ă→a, ș→s, ț→t, î→i, â→a) pentru a evita interpretarea greșită a paginii de cod.
- **Depășire de câmp:** dacă o valoare nu încape (sumă peste 15 poziții, denumire peste 60 de caractere), exportul **se oprește cu eroare explicită** — niciodată trunchiere silențioasă.

### 4.7 Numele fișierului exportat

```
IN_<data-inceput>_<data-sfarsit>_NT_<index>.dbf
```

- datele în format `dd-mm-yyyy`, corespunzând intervalului **selectat de utilizator** (nu datelor efectiv găsite);
- `NT` — literal constant;
- ✔ **DECIZIE D9 — `<index>` se scrie cu zerouri în față**, pe **4 poziții** (`0001`, `0042`, `1337`). Lățimea este o valoare de configurare; peste 9999 de exporturi numărul se scrie natural, fără trunchiere.

Exemplu: `IN_03-08-2026_31-08-2026_NT_0007.dbf`

> Fișierul exemplu primit, `IN_03-08-2026_03-08-2026_NT_index.dbf`, conține literalul `index` pentru că a fost generat manual, ilustrativ, cu o singură notă de transfer.

✔ **DECIZIE D10 — un export real conține toate liniile din intervalul selectat**, nu un eșantion. Pentru ANCAFARM1 / 03.08.2026 acest lucru înseamnă 7 linii de import → 14 înregistrări DBF.

---

## 5. Modelul de date (SQLite)

### 5.1 Tabela `linii_import`

| Coloană | Tip | Descriere |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | |
| `name` | TEXT (25) | gestiune sursă |
| `partner_name` | TEXT (25) | gestiune destinație |
| `doc_number` | TEXT (12) | |
| `doc_date` | TEXT | **format ISO `YYYY-MM-DD`** (obligatoriu pentru sortare și filtrare corectă) |
| `retail_vat_percent` | INTEGER | |
| `val_am_iesire` | INTEGER | **valoare în bani** (vezi nota) |
| `val_vat_am_iesire` | INTEGER | valoare în bani |
| `val_achizitie_fara_tva_iesire` | INTEGER | valoare în bani |
| `id_export` | INTEGER NULL | FK → `exporturi.id`; `NULL` = neexportat |
| `fisier_sursa` | TEXT | numele CSV-ului din care provine |
| `data_import` | TEXT | marcaj temporal |

```sql
CREATE UNIQUE INDEX ux_linii ON linii_import(name, doc_number, doc_date, retail_vat_percent);
CREATE INDEX ix_linii_filtru ON linii_import(name, doc_date, id_export);
```

> **Nota privind sumele:** valorile monetare se stochează ca **întregi în bani** (`96.04` → `9604`). **Nu se folosește `float` / `REAL`** — rotunjirile binare produc diferențe de un ban, inacceptabile într-un import contabil. În cod se lucrează cu tipul `decimal`.

### 5.2 Tabela `exporturi`

| Coloană | Tip | Descriere |
|---|---|---|
| `id` | INTEGER PK AUTOINCREMENT | **acesta este `<index>`-ul din numele fișierului** |
| `data_ora` | TEXT | momentul generării |
| `nume_fisier` | TEXT | numele complet al DBF-ului |
| `cale_fisier` | TEXT | unde a fost salvat |
| `filtru_gestiuni` | TEXT | lista gestiunilor selectate, separate prin `;` (D11) |
| `data_start`, `data_sfarsit` | TEXT | intervalul `DocDate` selectat |
| `nr_inregistrari` | INTEGER | linii din `linii_import` incluse |
| `nr_linii_dbf` | INTEGER | = `nr_inregistrari` × 2 |
| `total_valoare`, `total_tva` | INTEGER | pentru control (suma liniilor pozitive), în bani |
| `stare` | TEXT | `activ` / `anulat` |
| `observatii` | TEXT | notă liberă a operatorului |

Indexul nu se reutilizează niciodată: la anularea unui export, `id`-ul rămâne consumat, iar un nou export primește un număr nou. Astfel, numele fișierelor din folder rămân unice pe toată durata de viață a aplicației.

### 5.3 Tabele de configurare

- **`nomenclator_gestiuni`** — `denumire` (unic), `cont_marfa`, `activitate`, `activ`. Populată inițial cu tabelul din §4.4.
- **`denumiri_tva`** — `cota` (unic), `den_art`, `activ`. Populată cu valorile din §4.3.
- **`setari`** — perechi cheie/valoare: folder implicit CSV, folder implicit DBF, `COD` (implicit `20100`), `TIP` (implicit `A`), `UM` (implicit `BUC`), lățimea indexului (implicit `4`).

### 5.4 Regula la reimport

✔ **DECIZIE D3 — confirmată.** Pe cheia de unicitate:

| Situație | Acțiune |
|---|---|
| Linia nu există | **se inserează** |
| Există, `id_export IS NULL`, valori identice | se ignoră (nu este eroare) |
| Există, `id_export IS NULL`, valori diferite | **se actualizează**, se raportează în sumar |
| Există, `id_export IS NOT NULL`, valori identice | se ignoră |
| Există, `id_export IS NOT NULL`, valori **diferite** | **nu se modifică**, se semnalează ca **conflict** |

Ultimul caz este cel important: datele au fost deja predate în SAGA, iar o modificare tăcută în baza locală ar face ca cele două sisteme să nu mai poată fi reconciliate. Conflictele se afișează într-o listă separată, cu ambele seturi de valori (cea din bază și cea din CSV), pentru ca operatorul să decidă corecția manuală în SAGA.

Importul rulează într-o **singură tranzacție**: fie intră tot, fie nimic.

---

## 6. Interfața grafică

Cinci ecrane, accesibile din meniu sau din bară laterală. Interfața trebuie să facă evident, în orice moment, **câte linii sunt neexportate**.

### 6.1 Ecran „Import CSV”

- buton *Alege fișier…* + suport pentru **glisare și plasare** (drag & drop);
- deschidere implicită în `d:\nttosaga\files`;
- **previzualizare** a primelor ~20 de linii recunoscute, înainte de confirmare;
- validare prealabilă cu raportare în clar: coloane lipsă, date nevalide, gestiuni **necunoscute în nomenclator**;
- buton *Importă* activ doar dacă validarea a trecut;
- **sumar după import**, pe categorii, fiecare cu detaliu accesibil:
  - X linii noi inserate
  - Y linii ignorate (identice, existente)
  - Z linii actualizate
  - W **conflicte** (deja exportate, cu valori diferite)
  - V linii ignorate pentru `ValAmIesire = 0` (D2)

> Gestiunea necunoscută în nomenclator este cazul cel mai probabil de eșec în exploatare — se deschide un punct de lucru nou și denumirea lui apare în CSV înainte să fie configurată. Aplicația trebuie să propună direct adăugarea ei în nomenclator, cu câmpurile de cont și activitate de completat, nu doar să afișeze o eroare.

### 6.2 Ecran „Date importate”

- tabel cu toate liniile, filtre pe: gestiune (`Name`), interval `DocDate`, stare (exportat / neexportat), cotă TVA;
- coloană vizibilă cu **indexul exportului** sau „neexportat”;
- totaluri afișate în subsolul tabelului;
- căutare după `DocNumber`;
- ștergerea unei linii este posibilă **doar** dacă nu a fost exportată, cu confirmare.

### 6.3 Ecran „Export DBF”

Fluxul principal, în trei pași pe același ecran:

**1. Selecție**

✔ **DECIZIE D11 — selecția gestiunilor este multiplă.** Un export poate conține mai multe gestiuni sursă și rămâne valid pentru SAGA.

- listă cu bifă pentru gestiuni, cu butoanele *Selectează tot* / *Deselectează tot*;
- interval `DocDate` — două selectoare de dată, cu scurtături: *ziua curentă*, *luna curentă*, *luna trecută*;
- lângă fiecare gestiune se afișează **numărul de linii neexportate** din intervalul curent, ca operatorul să vadă dintr-o privire unde are de lucru.

**2. Previzualizare**

- numărul de linii găsite și numărul de înregistrări DBF rezultate (dublul);
- totalurile pe `VALOARE` și `TVA`;
- lista liniilor incluse, grupate pe gestiune;
- bifă **implicit activă**: *„Doar liniile neexportate”*.

**3. Generare**

- se afișează numele fișierului care va fi creat și calea completă, **înainte** de generare;
- după succes: confirmare + buton *Deschide folderul*.

Comportamente obligatorii:

- dacă selecția include linii deja exportate și bifa este dezactivată, se cere confirmare explicită, cu indicarea exporturilor anterioare afectate;
- dacă nu există nicio linie în selecție, butonul de generare este inactiv, cu explicație vizibilă (nu doar dezactivat mut);
- dacă o gestiune din liniile selectate — **sursă sau destinație** — nu are corespondent în nomenclator, exportul **se blochează** cu indicarea liniei vinovate. Atenție: `PartnerName` poate conține gestiuni care nu apar niciodată ca `Name`, deci verificarea trebuie făcută pe ambele coloane;
- scrierea în baza de date (marcarea `id_export`) și scrierea fișierului sunt **atomice**: fișierul se scrie mai întâi într-un fișier temporar, iar marcarea liniilor și redenumirea finală se fac doar dacă scrierea a reușit integral.

### 6.4 Ecran „Istoric exporturi”

- listă cu toate exporturile: index, dată/oră, gestiuni, interval, număr linii, totaluri, nume fișier, stare;
- *Regenerează fișierul* — rescrie același DBF, cu **același index**, din datele din bază (util dacă fișierul a fost șters, mutat sau pierdut);
- *Anulează exportul* — marchează exportul `anulat` și eliberează liniile (`id_export = NULL`) pentru un nou export. Se cere confirmare puternică, cu avertismentul explicit că, dacă fișierul a fost deja importat în SAGA, înregistrările trebuie șterse și acolo, altfel se vor dubla;
- deschidere folder / deschidere fișier.

### 6.5 Ecran „Setări și nomenclatoare”

- editarea nomenclatorului de gestiuni (adăugare, modificare, **dezactivare** — nu ștergere, pentru a nu invalida istoricul);
- editarea denumirilor de articole pe cotă de TVA, cu avertismentul că valoarea trebuie să corespundă exact nomenclatorului SAGA;
- valorile constante (`COD`, `TIP`, `UM`, lățimea indexului);
- folderele implicite;
- buton *Copie de siguranță a bazei de date* → copiază fișierul `.db` cu marcaj temporal în subfolderul `backup`.

---

## 7. Cerințe non-funcționale

| Domeniu | Cerință |
|---|---|
| Instalare | copiere de folder, fără instalator, fără drepturi de administrator; ideal un singur executabil |
| Amplasare | `d:\nttosaga\` — executabil; `d:\nttosaga\data\nttosaga.db` — baza; `d:\nttosaga\files\` — CSV și DBF; `d:\nttosaga\backup\` — copii; `d:\nttosaga\log\` — jurnal |
| Prima pornire | creează structura de foldere, baza de date și populează nomenclatoarele cu valorile implicite, fără intervenția utilizatorului |
| Limbă | integral română, inclusiv mesajele de eroare |
| Jurnalizare | fișier text zilnic: importuri, exporturi, erori — pentru diagnostic la distanță |
| Copii de siguranță | automat înainte de fiecare import și export (păstrare ultimele 30) |
| Volum | ordinul miilor de linii pe lună — nu impune optimizări speciale |
| Utilizatori | un singur utilizator la un moment dat (SQLite local, fără acces în rețea) |
| Robustețe | nicio excepție netratată afișată ca „stack trace”; orice eroare devine mesaj lizibil |

---

## 8. Tehnologii recomandate

| Opțiune | Avantaje | Dezavantaje |
|---|---|---|
| **C# / .NET 8 + WinForms** ✅ | limbaj matur, SQLite prin `Microsoft.Data.Sqlite`, tip `decimal` exact pentru bani, scriere DBF la nivel de octet fără dificultate, publicare într-un singur `.exe` autonom, unelte gratuite (Visual Studio Community) | necesită .NET SDK la dezvoltare |
| Delphi / Lazarus (Free Pascal) | tradițional puternic pe DBF (suport nativ), Lazarus este gratuit | ecosistem mai restrâns, mai greu de găsit succesor pentru mentenanță |
| Electron / Node.js | interfață modernă | pachet mare (~150 MB), exagerat pentru acest scop |
| Java + JavaFX | portabil | necesită runtime, integrare mai greoaie pe desktop Windows |

**Recomandare: C# / .NET 8 cu WinForms**, publicat ca executabil autonom (`self-contained, single-file`). Scrierea DBF se implementează direct pe octeți conform §4.6 — structura este simplă și fixă, iar o bibliotecă externă ar adăuga dependențe fără beneficiu.

---

## 9. Plan de dezvoltare pe etape

| Etapă | Conținut | Rezultat verificabil |
|---|---|---|
| 1 | Schema SQLite + nomenclatoare + prima pornire | baza se creează corect, populată cu valorile implicite |
| 2 | Importatorul CSV (fără interfață) | cele 37 de linii din exemplu intră corect |
| 3 | Generatorul DBF (fără interfață) | **fișier identic octet cu octet** cu exemplul, pentru aceeași intrare |
| 4 | Interfața: import + vizualizare | operatorul poate importa singur |
| 5 | Interfața: export + istoric | fluxul complet funcțional |
| 6 | Setări, nomenclatoare, copii de siguranță, jurnal | aplicație completă |
| 7 | Testare cu date reale, în paralel cu procedura manuală, o lună | validare în producție |

> Etapa 3 este cea critică. **Testul de acceptanță este compararea binară** a fișierului generat cu `IN_03-08-2026_03-08-2026_NT_index.dbf`, pentru aceeași linie de intrare (NT 4244 / 03.08.2026 / cota 21). Dacă cei 1294 de octeți coincid, formatul este garantat acceptat de SAGA și nu mai depinde de interpretarea specificației scrise.

---

## 10. Teste de acceptanță

| # | Test | Rezultat așteptat |
|---|---|---|
| T1 | Import CSV exemplu pe bază goală | 37 linii inserate, 0 erori |
| T2 | Reimport același CSV | 0 inserate, 37 ignorate, 0 erori |
| T3 | Export ANCAFARM1, 03.08.2026 – 03.08.2026 | 7 linii → 14 înregistrări DBF |
| T4 | Comparare binară a înregistrării NT 4244 cota 21 cu exemplul | identice, octet cu octet |
| T5 | Al doilea export pe același interval, bifa „doar neexportate” activă | 0 linii, buton inactiv, mesaj explicativ |
| T6 | Anulare export, apoi re-export | liniile redevin disponibile, **index nou**, fișier nou |
| T7 | Import CSV cu gestiune inexistentă în nomenclator | avertisment clar + propunere de adăugare |
| T8 | Export cu linii cu cotă 0% | `DEN_ART` = `Scutite de TVA`, `CONT` = `371.0000x.0` |
| T9 | Închiderea forțată a aplicației în timpul importului | baza rămâne consistentă (tranzacție anulată) |
| T10 | Verificarea sumelor pe fișier | total `VALOARE` pozitiv = total negativ; balanță zero |
| T11 | Export multiplu: ANCAFARM1 + ANCAFARM2 + LABORATOR, 03.08 – 21.08.2026 | un singur fișier, 37 linii → 74 înregistrări, sortate D8 |
| T12 | Export cu `DocNumber` de lungimi diferite (`9999` și `10000`) | ordinea numerică respectată, nu cea alfabetică |
| T13 | `PartnerName` necunoscut în nomenclator, `Name` cunoscut | exportul se blochează cu indicarea liniei |

---

## 11. Registrul deciziilor

Toate punctele deschise din versiunea 1.0 au fost soluționate de beneficiar la 23.08.2026.

| # | Întrebare | Decizie |
|---|---|---|
| D1 | Filtrare după prefixul `NT`? | **Nu se filtrează.** Toate liniile se importă |
| D2 | Liniile cu `ValAmIesire = 0` | **Se ignoră** la import, cu raportare în sumar |
| D3 | Comportament la reimport cu valori modificate | Conform regulilor din §5.4 |
| D4 | `SCADENT` | **Egal cu `DATA`** |
| D5 | `TVAI` | **`0`** |
| D6 | `DEN_ART` cota 21 | **`Parafarmaceutice TVA<>11%`** |
| D7 | Cont pentru cota 0 | **`371.0000x.0`** |
| D8 | Ordinea de sortare | `DocDate`, apoi `DocNumber`, apoi `RetailVatPercent` |
| D9 | Formatul indexului | **Cu zerouri în față**, 4 poziții implicit |
| D10 | Conținutul unui export | **Toate liniile** din intervalul selectat |
| D11 | Selecția gestiunii | **Multiplă**; export cu mai multe gestiuni sursă este valid |

### Puncte de urmărit în implementare

Nu sunt întrebări deschise, ci detalii unde o alegere greșită de implementare produce erori tăcute:

1. **`DEN_ART` se copiază literal** din tabelul de configurare — fără normalizare de majuscule sau spații. Caracterele `<` și `>` fac parte din denumire.
2. **Potrivirea gestiunilor pe șir complet**, nu pe prefix (`DEPOZIT` vs `DEPOZIT ANCA1`).
3. **Validarea nomenclatorului pe ambele coloane**, `Name` și `PartnerName`.
4. **Sumele niciodată în `float`.**
5. **Sortarea `DocNumber` numeric** când conține doar cifre.
