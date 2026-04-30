# SistemaF — Migrazione VB6 → C# (.NET 8)

## Stack tecnologico

| Layer | Tecnologia | Sostituisce (VB6) |
|---|---|---|
| UI | WPF + CommunityToolkit.Mvvm | Form VB6 (`.frm`) |
| Application | MediatR (CQRS) + FluentValidation | Logica nei Form |
| Domain | C# puro (no dipendenze) | Classi COM VB6 (`.cls`) |
| Infrastructure | EF Core 8 + SQL Server | ADODB + Access `.mdb` |
| Integrations | WCF/HttpClient | CSFWSENET.dll |
| Logging | Serilog | File `.log` VB6 |
| DI/Config | Microsoft.Extensions.Hosting | Registri COM + `App.config` |

---

## Struttura della soluzione

```
SistemaF.sln
├── src/
│   ├── Core/
│   │   ├── SistemaF.Domain          ← Entità, Value Objects, interfacce repository
│   │   └── SistemaF.Application     ← Comandi, Query, Validator (MediatR)
│   ├── Infrastructure/
│   │   ├── SistemaF.Infrastructure  ← EF Core, DbContext, Repository
│   │   └── SistemaF.Integration     ← Federfarma DPC/WebCare, STAR, TariffaXML
│   └── UI/
│       └── SistemaF.UI.WPF          ← ViewModels, Views XAML, DI startup
└── tests/
    ├── SistemaF.Domain.Tests
    └── SistemaF.Application.Tests
```

### Corrispondenza moduli VB6 → C#

| Modulo VB6 | Equivalente C# |
|---|---|
| `CSFLib` / `modSessione.bas` | `Domain/ValueObjects/SessioneOperatore.cs` |
| `CSFDichiarazioni.bas` | `Domain/Entities/Prodotto.cs` (enum e costanti) |
| `CSFOrdEmissione` | `Application/Ordini/Commands/EmittiOrdineCommand.cs` |
| `CSFOrdCommon` | `Domain/Entities/Ordine.cs` + `RigaOrdine.cs` |
| `CSFRicerca` | `Application/Prodotti/Queries/CercaProdottiQuery.cs` |
| `CSFMag` | `Domain/Entities/Prodotto.cs` (GiacenzaAttuale) |
| `CSFFatturazione` | `Domain/Entities/Fattura.cs` (da sviluppare) |
| `CSFWSENET` | `Integration/Federfarma/FederfarmaService.cs` |
| `CSFReport` | `Infrastructure/Reporting/` (FastReport) |
| `CSFProt` | Licensing tramite `Microsoft.Extensions.Licensing` |

---

## Piano di migrazione (Strangler Fig Pattern)

### Fase 1 — Database (settimane 1–4)
- Esportare i dati da Access `.mdb` a SQL Server
- Creare le migration EF Core da zero
- Verificare integrità referenziale dei dati

### Fase 2 — Domain + Application Layer (mesi 1–4)
- Riscrivere un modulo alla volta partendo da `Prodotto` e `Ordine`
- Ogni entità VB6 → classe C# con test xUnit
- Mantenere il VB6 in produzione durante la migrazione

### Fase 3 — Infrastructure (mesi 3–6)
- Implementare tutti i Repository EF Core
- Migrare i Web Service SOAP (rigenerare proxy da WSDL)
- Portare il motore di report (CSFReport → FastReport)

### Fase 4 — UI WPF (mesi 5–12)
- Costruire le Views WPF per ogni modulo completato
- Sostituire gradualmente la UI VB6 schermata per schermata
- Training utenti in parallelo

### Fase 5 — Cutover e dismissione VB6
- Testing completo in ambiente di collaudo
- Go-live progressivo per farmacia pilota
- Dismissione del codice VB6 legacy

---

## Avvio rapido

```bash
# 1. Clonare il repository
git clone ...

# 2. Configurare SQL Server in appsettings.json
#    "SistemaF": "Server=.;Database=SistemaF;Trusted_Connection=True;..."

# 3. Creare il database
cd src/Infrastructure/SistemaF.Infrastructure
dotnet ef database update

# 4. Avviare l'applicazione
cd src/UI/SistemaF.UI.WPF
dotnet run

# 5. Eseguire i test
cd tests/SistemaF.Domain.Tests
dotnet test
```

---

## Convenzioni di codice

- **Nullable reference types** abilitati ovunque (`<Nullable>enable</Nullable>`)
- **Record** per DTO e Value Objects immutabili
- **Sealed class** per entità e handler (evita ereditarietà accidentale)
- **Factory methods** statici invece di costruttori pubblici nelle entità
- **Domain Events** per comunicazione tra aggregati
- Nessuna logica di business nei ViewModel o nei Repository
