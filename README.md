# Web App Scratch

## HTTP Request/Response Flow

This project is a scratch implementation of a TCP-based HTTP server in C#.

The following diagram shows the complete flow of an HTTP request from the client to the server, through parsing and routing, and finally back to the client as an HTTP response.

```text
                    ┌──────────────┐
                    │    Client    │
                    └──────┬───────┘
                           │
                         TCP
                           │
                           ▼
                    ┌──────────────┐
                    │  TcpServer   │
                    └──────┬───────┘
                           │
                           ▼
                    NetworkStream
                           │
                           ▼
                 HttpRequestReader
                           │
                    ┌──────┴──────┐
                    ▼             ▼
               Header bytes    Body bytes
                    │             │
                    ▼             ▼
             HeaderParser     BodyParser
                    │             │
                    └──────┬──────┘
                           ▼
                    RequestContext
                           │
                           ▼
                        Router
                           │
                           ▼
                       Endpoint
                           │
                           ▼
                        Handler
                           │
                           ▼
                     Response String
                           │
                           ▼
                      UTF-8 bytes
                           │
                           ▼
                    NetworkStream
                           │
                           ▼
                         Client
```

## Flow Overview

### 1. Client

The client sends an HTTP request to the server over a TCP connection.

```text
Client
  │
  │ HTTP Request
  ▼
TcpServer
```

### 2. TcpServer

`TcpServer` accepts the incoming TCP connection and obtains a `NetworkStream`.

```text
TcpClient
   ↓
NetworkStream
```

The `NetworkStream` is used to read request bytes from the client and write response bytes back to the client.

### 3. HttpRequestReader

`HttpRequestReader` reads raw bytes from the `NetworkStream`.

Its responsibility is to separate the incoming HTTP request into:

- Header bytes
- Body bytes

```text
NetworkStream
      ↓
HttpRequestReader
      ↓
┌─────────────────┐
│ Header bytes    │
│ Body bytes      │
└─────────────────┘
```

### 4. HeaderParser

The header bytes are passed to `HttpHeaderParser`.

It converts the raw HTTP header bytes into a structured `RequestContext`.

```text
Header bytes
     ↓
HttpHeaderParser
     ↓
RequestContext
```

For example:

```http
GET /users HTTP/1.1
Host: localhost:8080
Content-Type: application/json
```

is converted conceptually into:

```text
RequestContext
├── method  = "GET"
├── path    = "/users"
├── version = "HTTP/1.1"
└── Headers
    ├── Host
    └── Content-Type
```

### 5. BodyParser

The body bytes are passed to `HttpBodyParser`.

It decodes the bytes using UTF-8 and returns a string.

```text
Body bytes
    ↓
HttpBodyParser
    ↓
String
```

### 6. RequestContext

After parsing the header and body, the request is represented by a `RequestContext`.

```text
RequestContext
├── method
├── path
├── version
├── Headers
└── Body
```

This object carries the request information to the next layer.

### 7. Router

The `Router` receives the `RequestContext` and searches the registered endpoints.

```text
RequestContext
      ↓
    Router
      ↓
Endpoint matching
```

The router checks things such as:

```text
HTTP Method
+
Request Path
```

to find the correct endpoint.

### 8. Endpoint

An `Endpoint` represents a route and its handler.

Conceptually:

```text
Endpoint
├── Path
├── Method
└── Handler
```

For example:

```text
GET /users
     ↓
GetUsers handler
```

### 9. Handler

Once a matching endpoint is found, its handler is executed.

```text
Endpoint
    ↓
Handler
    ↓
Response
```

The handler produces a response string.

### 10. Build HTTP Response

The response string is converted into a complete HTTP response.

Conceptually:

```http
HTTP/1.1 200 OK
Content-Length: ...
X-Name: Mredul

Hello Users
```

The response is then encoded using UTF-8.

```text
Response String
      ↓
Encoding.UTF8.GetBytes()
      ↓
byte[]
```

### 11. NetworkStream → Client

Finally, the response bytes are written to the `NetworkStream`.

```text
Response bytes
      ↓
NetworkStream
      ↓
TCP
      ↓
Client
```

## Complete Mental Model

The server processing pipeline can be summarized as:

```text
READ
  ↓
Parse
  ↓
Create RequestContext
  ↓
Route
  ↓
Execute Handler
  ↓
Build Response
  ↓
Encode to bytes
  ↓
WRITE
```

### One-Line Summary

> **Read → Parse → Build Context → Route → Execute Handler → Build HTTP Response → Write to Client**
