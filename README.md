
# LLM-Communication Service

This service is responsible for handling natural language input using **Claude (Anthropic LLM)** and converting it into actionable **intent + parameters**, which are then dispatched to the correct endpoint via API Gateway.

---

## 🧠 Purpose

- Accept user input (e.g., "Pay my gas bill for March")
- Call Claude API with a structured prompt
- Extract:
  - `intent`: What the user wants (e.g., `MakePayment`)
  - `parameters`: Required info (e.g., `month`, `year`, `amount`)
- Dispatch to the API Gateway using HTTP POST

---

## 🛠️ Tech Stack

- **.NET 8 Web API**
- **HttpClient** for Claude API calls
- **Regex** to extract JSON from Claude's raw response
- **Ocelot-compatible** gateway dispatcher

---

## 📦 Project Structure

```
/Services
  ClaudeService.cs         → handles Claude requests and dispatch logic
/Models
  LLMRequest.cs, LLMResponse.cs → request/response DTOs
/Controllers
  LLMCommController.cs     → entry point for frontend or UI
```

---

## 🔄 Workflow

1. **Frontend sends message** ➝ `/api/llm`
2. **ClaudeService** sends prompt to Claude API
3. Claude replies with JSON containing:
   - `"intent": "MakePayment"`
   - `"parameters": { "month": "5", "year": "2024", ... }`
4. Service forwards this to the API Gateway via a mapped endpoint

---

## 🔐 Claude Prompt (Embedded)

Claude is asked to return **only valid JSON**. Example:

```
{
  "intent": "QueryBillDetailed",
  "parameters": {
    "subscriberId": "1234",
    "month": "5",
    "year": "2024"
  }
}
```

---

## 🚀 Dispatch Logic

Mapped routing to gateway:

| Intent             | Gateway Path                  |
|--------------------|-------------------------------|
| `QueryBill`        | `/gateway/querybill`          |
| `QueryBillDetailed`| `/gateway/querybill-detailed` |
| `MakePayment`      | `/gateway/paybill`            |

---

## 🧪 Example Request

```json
{
  "message": "Show me my electricity bill for April"
}
```

---

## ✅ Example Response

```json
{
  "intent": "QueryBill",
  "parameters": {
    "subscriberId": "1234",
    "month": "4",
    "year": "2024"
  }
}
```

---

### Video Link
https://youtu.be/3O6gRAq8Scw


## 👥 Authors

Developed by **Yigit Alp YUKSEL** – SE4458 Assignment 2
