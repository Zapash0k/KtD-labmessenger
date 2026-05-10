# Messenger API — Lab 2
### Software Design and Documentation
**Variant 4 — Group Chat** | C# / .NET 8 / SQLite

---

## Description

A working prototype of a messenger system built on top of the architecture designed in **Lab 1 (Variant 4 – Group Chat)**.

The system implements:
- **Minimal architecture**: users, conversations, message persistence, REST API
- **Variant 4 – Group Chat**: fan-out delivery, per-recipient `DeliveryRecord`, aggregated message status lifecycle

---

## Project Structure

```
Messenger.sln
├── MessengerApi/                  # Main Web API project
│   ├── Models/                    # Domain entities
│   │   ├── User.cs
│   │   ├── Conversation.cs
│   │   ├── ConversationMember.cs
│   │   ├── Message.cs
│   │   └── DeliveryRecord.cs      # Per-recipient status (Variant 4)
│   ├── Services/                  # Business logic
│   │   ├── Dtos.cs                # Request/Response DTOs
│   │   ├── UserService.cs
│   │   ├── ConversationService.cs
│   │   └── MessageService.cs      # Fan-out logic (Variant 4)
│   ├── Storage/
│   │   └── AppDbContext.cs        # EF Core + SQLite
│   ├── Api/
│   │   ├── UsersController.cs
│   │   ├── ConversationsController.cs
│   │   ├── MessagesController.cs
│   │   └── ErrorHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── postman_collection.json        # Postman collection
```

---

## How to Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Start the API
```bash
cd MessengerApi
dotnet run
```

The server starts at `http://localhost:5000`.  
Swagger UI is available at `http://localhost:5000/swagger`.

The SQLite database file (`messenger.db`) is created automatically on first run.

### Run Tests
```bash
dotnet test
```

---

## API Endpoints

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/users` | Create a user |
| `GET` | `/users` | List all users |
| `GET` | `/users/{id}` | Get user by ID |

### Conversations
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/conversations/direct` | Create 1-on-1 conversation |
| `POST` | `/conversations/group` | Create group conversation *(Variant 4)* |
| `GET` | `/conversations/{id}` | Get conversation by ID |
| `GET` | `/conversations/user/{userId}` | Get user's conversations |
| `GET` | `/conversations/{id}/messages` | Get message history |

### Messages
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/messages` | Send a message |
| `POST` | `/messages/{id}/deliver` | Acknowledge delivery *(Variant 4)* |
| `POST` | `/messages/{id}/read` | Mark as read *(Variant 4)* |

---

## Data Model

```
User           Conversation (Direct | Group)
 └─ many ──── ConversationMember ─────── many ┘

Message
 ├─ belongsTo Conversation
 ├─ belongsTo User (sender)
 └─ hasMany   DeliveryRecord    ← one per recipient (Variant 4)
```

### Message Status (aggregated)
```
Sent → PartiallyDelivered → Delivered → PartiallyRead → Read
```

### DeliveryRecord Status (per-recipient)
```
Pending → Delivered → Read
         ↘ Failed
```

---

## Variant 4 — Group Chat: Fan-out Flow

When a message is sent to a group:
1. Message is saved once in the database
2. A `DeliveryRecord` with status `Pending` is created for **each recipient** (everyone except the sender)
3. The aggregated message status is recalculated after every delivery/read acknowledgement

---

## Postman Collection

Import `postman_collection.json` into Postman.

The collection includes:
- Create users (Alice, Bob, Carol)
- Create direct and group conversations
- Send messages
- Acknowledge delivery (per recipient)
- Mark as read (per recipient)
- Get message history
- Error case testing (404, 400, 403)

**Run order:** Execute requests top-to-bottom; collection variables (`userId1`, `groupConvId`, `messageId`, etc.) are set automatically by test scripts.

---

## Architecture

Follows the design from **Lab 1 – Variant 4**:

```
Client / Postman
     │
     ▼
HTTP API (ASP.NET Core Controllers)
     │
     ▼
Services (UserService, ConversationService, MessageService)
     │
     ▼
EF Core AppDbContext
     │
     ▼
SQLite (messenger.db)
```

---

## Defense Questions

1. **How are messages not lost?**  
   Every message is persisted to SQLite via EF Core before the API responds. The `202 Accepted` response is only sent after the database write succeeds.

2. **What happens if the recipient is offline?**  
   The `DeliveryRecord` stays in `Pending` status. When the client reconnects and acknowledges, it calls `POST /messages/{id}/deliver` to update the status.

3. **How are messages uniquely identified?**  
   Each message gets a `Guid`-based `Id` generated at creation time, stored as a string primary key.

4. **What errors can occur when sending a message?**  
   - `400 Bad Request` — empty text
   - `404 Not Found` — unknown `senderId` or `conversationId`
   - `403 Forbidden` — sender is not a member of the conversation

5. **How would the system scale to 1 million users?**  
   - Replace SQLite with PostgreSQL
   - Introduce a message queue (RabbitMQ / Kafka) for fan-out instead of synchronous DB writes
   - Add a WebSocket gateway for real-time delivery
   - Horizontal scaling of the API with stateless services
   - Batch fan-out jobs for large groups
