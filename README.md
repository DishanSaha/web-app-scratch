# Web App Scratch — TCP Server from Scratch

> একটি simplified **ASP.NET Core** — নিজের হাতে TCP, HTTP, DI, Middleware, Model Binder, Controller সব বানানো।
>
> **শেখার লক্ষ্য:** Framework-এর ভিতরে আসলে কী ঘটে সেটা বোঝা। Blindly framework ব্যবহার না করে, ভিতরের mechanism নিজে লিখে শেখা।

---

## 📖 সূচিপত্র

- [এই project কী?](#এই-project-কী)
- [Big Picture — সব একসাথে](#big-picture--সব-একসাথে)
- [Layer by Layer ব্যাখ্যা](#layer-by-layer-ব্যাখ্যা)
- [Startup Flow — App চালু হলে কী হয়](#startup-flow--app-চালু-হলে-কী-হয়)
- [Request Flow — একটি request আসলে কী হয়](#request-flow--একটি-request-আসলে-কী-হয়)
- [File Structure](#file-structure)
- [কীভাবে চালাবেন](#কীভাবে-চালাবেন)
- [Test করার Example](#test-করার-example)
- [আমরা যা শিখলাম](#আমরা-যা-শিখলাম)
- [Real ASP.NET Core-এর সাথে তুলনা](#real-aspnet-core-এর-সাথে-তুলনা)

---

## এই project কী?

এটি একটি **mini web framework** যা আপনি নিজে বানিয়েছেন। এটি:

- TCP দিয়ে client connection নেয়
- HTTP request parse করে
- Middleware pipeline-এ চালায়
- Router দিয়ে endpoint খোঁজে
- DI দিয়ে dependencies resolve করে
- Model Binder দিয়ে JSON → C# object বানায়
- Controller-এর action invoke করে
- HTTP response ফেরত পাঠায়

**এটি আসল ASP.NET Core-এর মতোই conceptually কাজ করে**, কিন্তু অনেক simplified — শেখার জন্য।

---

## Big Picture — সব একসাথে

```text
┌──────────────────────────────────────────────────────────────────┐
│                     CLIENT (Browser / curl)                      │
│                                                                  │
│        GET /products HTTP/1.1                                    │
│        Host: localhost:5005                                      │
└────────────────────────┬─────────────────────────────────────────┘
                         │ TCP Connection
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                       TcpServer                                  │
│   ┌─────────────────────────────────────────────────────────┐    │
│   │ 1. TcpListener.AcceptTcpClientAsync()                   │    │
│   │ 2. client.GetStream() → NetworkStream                   │    │
│   │ 3. HttpRequestReader.ReadAsync(stream)                  │    │
│   │ 4. HttpHeaderParser / HttpBodyParser                    │    │
│   │ 5. await _pipeline(context)                             │    │
│   │ 6. Response bytes → NetworkStream                       │    │
│   └─────────────────────────────────────────────────────────┘    │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    MIDDLEWARE PIPELINE                           │
│                                                                  │
│   ╔═══════════════ Logging ════════════════╗                     │
│   ║  ╔═══════════ Timing ════════════╗    ║                     │
│   ║  ║  ╔════ Endpoint Executor ════╗ ║    ║                     │
│   ║  ║  ║   router.Resolve(ctx)      ║ ║    ║                     │
│   ║  ║  ╚════════════════════════════╝ ║    ║                     │
│   ║  ╚═════════════════════════════════╝   ║                     │
│   ╚═════════════════════════════════════════╝                    │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                        ROUTER                                    │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │  Endpoint খুঁজে: Path + HttpMethod match করে             │   │
│   │         ↓                                                │   │
│   │  HandlerInvoker.InvokeMethod(method, target, context)    │   │
│   └──────────────────────────────────────────────────────────┘   │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    HANDLER INVOKER                               │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │  For each parameter:                                     │   │
│   │    1. RequestContext? → context                          │   │
│   │    2. DI-registered? → provider থেকে                      │   │
│   │    3. JSON body? → Model Binder                          │   │
│   │         ↓                                                │   │
│   │  method.Invoke(target, args) → string                    │   │
│   └──────────────────────────────────────────────────────────┘   │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────────────────┐
│                  CONTROLLER / HANDLER                            │
│        ProductController.GetAll()  →  "All products..."          │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼ Response ফিরে আসে
                  HTTP/1.1 200 OK
                  Content-Length: N

                  All products: [Laptop, Phone]
```

---

## Layer by Layer ব্যাখ্যা

### 🟦 Layer 1: TCP (Transport)

**File:** `TcpServer.cs`, `HttpRequestReader.cs`

**কাজ:** Client থেকে bytes পড়া, response bytes পাঠানো।

```text
Client ──TCP connection──▶ TcpListener
                              │
                              ▼
                          TcpClient
                              │
                              ▼
                         NetworkStream
                              │
                              ▼
                          byte[] data
```

**মূল concept:**

- **TcpListener** — নির্দিষ্ট port-এ wait করে, client connection accept করে
- **TcpClient** — একটি established connection
- **NetworkStream** — bytes পড়া ও লেখার stream
- TCP একটি **stream** — data টুকরো টুকরো আসে, একবারে না

---

### 🟨 Layer 2: HTTP (Application)

**File:** `HttpHeaderParser.cs`, `HttpBodyParser.cs`, `HttpRequestReader.cs`

**কাজ:** Raw bytes → structured `RequestContext`।

```text
Raw bytes:
GET /products HTTP/1.1\r\n
Host: localhost:5005\r\n
\r\n
        │
        ▼
HttpRequestReader
   - \r\n\r\n খুঁজে header শেষ বের করা
   - Content-Length পড়ে body size জানা
        │
        ▼
HttpHeaderParser
   - Request line: method, path, version
   - Headers: key-value
        │
        ▼
RequestContext {
   method = "GET",
   path = "/products",
   version = "HTTP/1.1",
   Headers = { Host: "..." },
   Body = ""
}
```

**মূল concept:**

- HTTP হলো TCP-এর উপরে একটি **text-based protocol**
- প্রতিটি line শেষে `\r\n` (CRLF)
- Header শেষ হয় `\r\n\r\n` দিয়ে (একটি ফাঁকা line)
- `Content-Length` header body-র size বলে

---

### 🟩 Layer 3: Middleware Pipeline

**File:** `Middleware/PipelineBuilder.cs`

**কাজ:** Request কে multiple function-এর মধ্য দিয়ে pass করানো।

**Onion Model:**

```text
        ┌─────────────────────────────┐
        │      Logging                │
        │   ┌─────────────────────┐   │
        │   │     Timing          │   │
        │   │   ┌─────────────┐   │   │
        │   │   │  Endpoint   │   │   │
        │   │   │  Executor   │   │   │
        │   │   └─────────────┘   │   │
        │   └─────────────────────┘   │
        └─────────────────────────────┘

Request ──▶ ভিতরে যায়
Response ◀── বাইরে আসে
```

**প্রতিটি middleware:**

```csharp
async Task Middleware(RequestContext ctx, Func<RequestContext, Task> next)
{
    // Before: request ভিতরে যাচ্ছে
    await next(ctx);   // ← পরের middleware
    // After: response বাইরে আসছে
}
```

**কেন reverse iteration দরকার?**

`Build()` method-এ pipeline তৈরি হয় শেষ থেকে প্রথমে:

```text
Start:  terminal = Task.CompletedTask
Step 1: pipeline = C(terminal)         [C সবচেয়ে শেষ middleware]
Step 2: pipeline = B(pipeline)         [B C-কে wrap করে]
Step 3: pipeline = A(pipeline)         [A B-কে wrap করে]

Result: A → B → C → terminal
```

এতে execution order সঠিক হয়।

---

### 🟪 Layer 4: Router

**File:** `Core/Router.cs`, `Core/Endpoint.cs`

**কাজ:** কোন request-এ কোন handler call হবে সেটা ঠিক করা।

```text
RequestContext { method="GET", path="/products" }
        │
        ▼
Router._endpoints = [
   Endpoint("/test", "GET", ...),
   Endpoint("/users", "GET", ...),
   Endpoint("/products", "GET", ...),   ← এটা match
   Endpoint("/products", "POST", ...),
]
        │
        ▼
FirstOrDefault(ep => ep.Matches(context))
        │
        ▼
match পাওয়া গেল → HandlerInvoker-এ পাঠাও
```

**Matching rules:**

- Path: `context.path.StartsWith(Path)` (prefix match)
- Method: `context.method.Equals(HttpMethod)` (case-insensitive)

---

### 🟫 Layer 5: DI Container

**File:** `DI/*.cs`

**কাজ:** Class-গুলোর dependencies resolve করা।

```text
CustomServiceCollection:
   AddTransient<ITest, Test>()  → list-এ যোগ
        │
        ▼
ServiceProvider:
   GetRequiredService<ITest>()
        │
        ├── descriptor খুঁজে
        ├── constructor-এর parameters দেখে
        ├── recursively resolve করে
        └── Activator.CreateInstance → object
```

**Lifetime:**

| Lifetime      | কখন তৈরি হয়    | কতবার            |
| ------------- | --------------- | ---------------- |
| **Transient** | প্রতি resolve-এ | নতুন instance    |
| **Scoped**    | প্রতি request-এ | একটাই (scope-এ)  |
| **Singleton** | প্রথম resolve-এ | পুরো app-এ একটাই |

---

### 🟧 Layer 6: Model Binder

**File:** `ModelBinder/JsonModelBinder.cs`, `ModelBinder/HandlerInvoker.cs`

**কাজ:** HTTP request-এর raw data → handler-এর typed parameter।

```text
Handler: Create(Product product) { ... }
                   ↑
                   │ Model Binder
                   │
Body: {"Name":"iPhone","Price":999}
        │
        ▼
JsonSerializer.Deserialize<Product>(body)
        │
        ▼
Product { Name="iPhone", Price=999 }
```

**Parameter resolution order:**

1. `RequestContext` type? → context-ই দাও
2. DI-তে registered? → DI থেকে দাও
3. Body JSON? → Model Binder দিয়ে parse করো
4. কিছুই না? → null

---

### 🟥 Layer 7: Controller

**File:** `Core/ControllerDiscovery.cs`, `Controllers/ProductController.cs`

**কাজ:** Attribute-based routing, action methods।

```csharp
public class ProductController
{
    [HttpGet("/products")]
    public string GetAll() => "All products";

    [HttpPost("/products")]
    public string Create(Product product)
        => $"Created: {product.Name}";
}
```

**Discovery (startup-এ একবার):**

```text
ControllerDiscovery.Discover(typeof(ProductController))
    │
    ├── new ProductController()
    │
    ├── Scan all methods
    │
    ├── GetAll-এ [HttpGet("/products")] আছে?
    │      → Endpoint("/products", "GET", GetAll, instance)
    │
    └── Create-এ [HttpPost("/products")] আছে?
           → Endpoint("/products", "POST", Create, instance)
```

**Attribute naming convention:**

```csharp
[HttpGet]           → compiler খোঁজে HttpGetAttribute
[HttpPost]          → compiler খোঁজে HttpPostAttribute
[Obsolete]          → compiler খোঁজে ObsoleteAttribute
```

C#-এ attribute class-এর নাম শেষে `Attribute` থাকলে use-এ বাদ দেওয়া যায়।

---

## Startup Flow — App চালু হলে কী হয়

```text
1. Program.cs শুরু
   │
   ├── Middleware functions define (Logging, Timing)
   │
   ├── builder = WebApplicationFactory.CreateBuilder()
   │       └── Services = new CustomServiceCollection()
   │
   ├── builder.Services.AddTransient<ITest, Test>()
   │       └── ServiceDescriptor(ITest, Test, Transient) list-এ যোগ
   │
   ├── app = builder.Build()
   │       ├── ServiceProvider তৈরি
   │       ├── HandlerInvoker তৈরি
   │       ├── Router তৈরি (invoker সহ)
   │       └── PipelineBuilder তৈরি
   │
   ├── app.Use(Logging)
   │       └── _pipelineBuilder._middlewares.Add(Logging)
   │
   ├── app.Use(Timing)
   │       └── _pipelineBuilder._middlewares.Add(Timing)
   │
   ├── app.MapGet("/test", handler)
   │       └── _router._endpoints.Add(Endpoint("/test", "GET", handler))
   │
   ├── app.MapGet("/users", handler)
   │       └── _router._endpoints.Add(...)
   │
   ├── app.AddControllers(typeof(ProductController))
   │       └── ControllerDiscovery.Discover(typeof(ProductController))
   │             └── endpoints list-এ ২টা endpoint যোগ
   │
   └── await app.RunAsync(5005)
           ├── PipelineBuilder.Use(endpoint-executor)  ← সবচেয়ে ভিতরে
           ├── pipeline = PipelineBuilder.Build()
           │       └── Reverse iteration: Logging → Timing → Endpoint → Terminal
           ├── new TcpServer(5005, pipeline)
           │
           └── TcpListener.Start()
                   → "Listening on port 5005"
                   → while(true) AcceptTcpClientAsync()
```

---

## Request Flow — একটি request আসলে কী হয়

```text
curl.exe http://localhost:5005/products
    │
    ▼
1. OS: TCP connection accepted
    │
    ▼
2. TcpServer.HandleClient
    │
    ├── stream = client.GetStream()
    ├── (header, body) = await HttpRequestReader.ReadAsync(stream)
    │       └── loop যতক্ষণ \r\n\r\n পাওয়া যায়
    │
    ├── context = HttpHeaderParser.Parse(header)
    │       └── RequestContext { method="GET", path="/products" }
    │
    ├── context.Body = HttpBodyParser.Parse(body)
    │
    └── await _pipeline(context)
            │
            ▼
3. Middleware Pipeline
    │
    ├── [Logging] before
    │       │
    │       ▼
    │   [Timing] before
    │       │
    │       ▼
    │   [Endpoint-executor]
    │       ├── response = router.Resolve(context)
    │       │       │
    │       │       ▼
    │       │   Router.Resolve
    │       │       ├── endpoint = _endpoints.FirstOrDefault(match)
    │       │       │       → ProductController.GetAll
    │       │       └── _invoker.InvokeMethod(method, target, context)
    │       │               │
    │       │               ▼
    │       │           HandlerInvoker
    │       │               ├── parameters = []
    │       │               ├── method.Invoke(instance, [])
    │       │               │       └── return "All products..."
    │       │               └── return "All products..."
    │       │
    │       ├── ctx.Response = response
    │       └── await next(ctx) → Terminal
    │
    ├── [Timing] after → print elapsed
    │
    └── [Logging] after → print response
    │
    ▼
4. TcpServer: Response bytes
    │
    ├── bodyBytes = UTF8.GetBytes(ctx.Response)
    ├── headerBytes = UTF8.GetBytes("HTTP/1.1 200 OK\r\n...")
    └── stream.WriteAsync(headerBytes + bodyBytes)
    │
    ▼
5. Client response পেল
```

---

## File Structure

```text
web_app_scratch/
│
├── Program.cs                    ← Entry point, app setup, middleware def
├── README.md                     ← এই file
│
├── TcpServer.cs                  ← TCP listener, accept, response
├── RequestContext.cs             ← Request data model
├── HttpException.cs              ← Custom exception
├── HttpRequestReader.cs          ← Bytes → header + body
├── HttpHeaderParser.cs           ← Header bytes → RequestContext
├── HttpBodyParser.cs             ← Body bytes → string
│
├── Attributes/
│   └── HttpMethodAttributes.cs   ← [HttpGet], [HttpPost]
│
├── Controllers/
│   └── ProductController.cs      ← Controller class
│
├── Models/
│   ├── User.cs                   ← User model
│   └── Product.cs                ← Product model
│
├── Services/
│   ├── ITest.cs
│   └── Test.cs                   ← DI example service
│
├── Middleware/
│   └── PipelineBuilder.cs        ← Middleware pipeline
│
├── ModelBinder/
│   ├── JsonModelBinder.cs        ← JSON → C# object
│   └── HandlerInvoker.cs         ← Parameter resolve + invoke
│
├── DI/
│   ├── CustomServiceCollection.cs ← Registration
│   ├── ServiceDescriptor.cs       ← Mapping record
│   ├── ServiceLifetime.cs         ← Transient/Scoped/Singleton
│   ├── ServiceProvider.cs         ← Resolution
│   └── ServiceScope.cs            ← Scope management
│
└── Core/
    ├── Endpoint.cs                ← Route endpoint
    ├── Router.cs                  ← Path matching
    ├── ControllerDiscovery.cs     ← Find controllers via reflection
    └── MiniWebApplication.cs      ← App builder
```

---

## কীভাবে চালাবেন

### Prerequisites

- **.NET 8** অথবা তার উপরে
- Windows / Linux / Mac

### Run

**Terminal 1 — Server চালু:**

```powershell
cd "C:\Users\USER\Web App Scratch (internal)\web_app_scratch"
dotnet run
```

Expected:

```
Listening on port 5005
```

**Terminal 2 — Requests পাঠান:**

```powershell
# Lambda endpoint
curl.exe http://localhost:5005/test

# Model binder test
curl.exe -X GET http://localhost:5005/users `
  -H "Content-Type: application/json" `
  -d "{\"Name\":\"Mredul\",\"Age\":25}"

# Controller GET
curl.exe http://localhost:5005/products

# Controller POST
curl.exe -X POST http://localhost:5005/products `
  -H "Content-Type: application/json" `
  -d "{\"Name\":\"iPhone\",\"Price\":999}"
```

---

## Test করার Example

### Example 1 — Simple GET

```powershell
curl.exe -v http://localhost:5005/products
```

**Response:**

```
> GET /products HTTP/1.1
> Host: localhost:5005

< HTTP/1.1 200 OK
< Content-Length: 28
< X-Name : Mredul

All products: [Laptop, Phone]
```

### Example 2 — POST with JSON

```powershell
curl.exe -X POST http://localhost:5005/products `
  -H "Content-Type: application/json" `
  -d "{\"Name\":\"iPhone\",\"Price\":999}"
```

**Response:**

```
Created product: iPhone ($999)
```

### Example 3 — Model Binder

```powershell
curl.exe -X GET http://localhost:5005/users `
  -H "Content-Type: application/json" `
  -d "{\"Name\":\"Rahim\",\"Age\":30}"
```

**Response:**

```
User: Rahim, Age: 30
```

### Server Log (Terminal 1)

প্রতিটি request-এ server terminal-এ এমন output আসবে:

```
[Logging] --> GET /products
[Timing] /products took 0ms
[Logging] <-- /products response: All products: [Laptop, Phone]

[Logging] --> POST /products
[Timing] /products took 1ms
[Logging] <-- /products response: Created product: iPhone ($999)
```

---

## আমরা যা শিখলাম

এই project-এ নিজের হাতে বানানো হয়েছে:

| #   | Component         | কী শেখা হয়েছে                                        |
| --- | ----------------- | ----------------------------------------------------- |
| 1   | TCP Server        | Listener, Accept, NetworkStream                       |
| 2   | HTTP Parser       | `\r\n\r\n`, Content-Length, header/body split         |
| 3   | RequestContext    | HTTP data model                                       |
| 4   | Router            | Path + method matching                                |
| 5   | Endpoint          | Route abstraction                                     |
| 6   | Response Writer   | Status line, headers, body                            |
| 7   | DI Container      | ServiceDescriptor, ServiceCollection, ServiceProvider |
| 8   | DI Lifetimes      | Transient, Scoped, Singleton                          |
| 9   | Reflection        | Activator, MethodInfo, parameters                     |
| 10  | Middleware        | Onion model, delegate chain, reverse iteration        |
| 11  | Model Binder      | JSON → C# object                                      |
| 12  | Handler Invoker   | Parameter resolution (context, DI, binder)            |
| 13  | Attribute Routing | `[HttpGet]`, `[HttpPost]`                             |
| 14  | Controller        | Action methods, discovery                             |

**এটা একটা real web framework-এর core concept।**

---

## Real ASP.NET Core-এর সাথে তুলনা

| Feature          | এই Project                               | Real ASP.NET Core                          |
| ---------------- | ---------------------------------------- | ------------------------------------------ |
| TCP Server       | `TcpListener`                            | Kestrel                                    |
| HTTP Parser      | Manual loop                              | Optimized parser                           |
| HTTP/2, HTTP/3   | ❌                                       | ✅                                         |
| Middleware       | `Func<RequestContext, Func<Task>, Task>` | `Func<HttpContext, RequestDelegate, Task>` |
| Pipeline Builder | `PipelineBuilder`                        | `ApplicationBuilder`                       |
| DI               | Custom ServiceProvider                   | `Microsoft.Extensions.DependencyInjection` |
| Model Binder     | JSON → object                            | Complex (query, route, form)               |
| Routing          | `StartsWith`                             | Segment-based, constraints                 |
| Controllers      | `[HttpGet]`                              | `[HttpGet]` + `[ApiController]` + Filters  |
| Response         | `string`                                 | `IActionResult` + content negotiation      |
| Async            | `async/await`                            | একই + Cancellation tokens                  |
| Logging          | `Console.WriteLine`                      | `ILogger<T>`                               |
| Config           | Hard-coded                               | `appsettings.json`                         |

**Conceptual structure একই।** আমরা ছোট করে দেখলাম যা ASP.NET Core বড় করে করে।

---

## পরবর্তী Steps (Learning Path)

### Path 1 — Route Parameters & Query String

```csharp
[HttpGet("/products/{id}")]
public string Get(int id) => $"Product {id}";
```

### Path 2 — Async Handlers

```csharp
[HttpGet("/users")]
public async Task<string> GetUsers() { ... }
```

### Path 3 — IActionResult

```csharp
return Ok(data);           // 200
return NotFound();         // 404
return BadRequest(error);  // 400
```

### Path 4 — Concurrency

একসাথে একাধিক client handle করা:

```csharp
_ = HandleClient(client);   // await ছাড়াই
```

### Path 5 — Configuration

`appsettings.json` থেকে settings পড়া।

---

## Contributing / Fork

এই project শেখার জন্য। যেকোনো নতুন feature যোগ করুন, break করুন, আবার ঠিক করুন — এটাই শেখার সেরা পথ।

**Mindset:**

> 🎯 **"Framework কীভাবে কাজ করে"** জানার চেয়ে **"নিজে বানিয়ে দেখা"** অনেক ভালো শেখায়।
>
> এই project সেটাই।

---

## License

Learning project — কোন License নেই। যা খুশি করো।

---

**Happy Learning! 🚀**
