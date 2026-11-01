# Coddify Backend

ASP.NET Core (.NET 10) Web API for Coddify, with Clean Architecture, EF Core code-first on SQL Server, and JWT auth. The Angular frontend (`Coddify_Frontend`) calls it at `http://localhost:5070`.

## Run it locally

1. **SQL Server Express** must be running as `localhost\SQLEXPRESS` (see `ConnectionStrings` in `Coddify_Backend/appsettings.json`).
2. **Secrets** live in user-secrets, never in the repo. Set them once from `Coddify_Backend/`:
   ```bash
   dotnet user-secrets set "JWT:Secret" "<at least 32 random characters>"
   dotnet user-secrets set "SmtpSettings:SenderEmail" "<gmail address>"
   dotnet user-secrets set "SmtpSettings:Password" "<gmail app password>"
   dotnet user-secrets set "Seed:AdminEmail" "admin@coddify.dev"
   dotnet user-secrets set "Seed:AdminPassword" "<admin password>"
   # Optional: real AI tutor answers (otherwise built-in hints are used)
   dotnet user-secrets set "AI:ApiKey" "<Anthropic API key>"
   ```
3. Start the API:
   ```bash
   cd Coddify_Backend
   dotnet run --launch-profile http
   ```
   On start it applies migrations, creates `CoddifyDB` if needed and seeds the starter content from `Infrastructure/Seed/Data/*.json`: 28 courses, 362 problems, 59 cosmetics, quotes and the admin account. Seeding only fills empty tables, so content edited by admins is never overwritten.
4. Swagger UI: http://localhost:5070/. Log in with `/api/auth/login`, then press **Authorize** and paste the `accessToken`.

Without SMTP settings, registration still works: the verification and reset links are written to the API log.

## Structure

| Project | Holds |
|---|---|
| `Coddify_Backend` | Controllers (`Controllers/<Module>`), `InjectedServices` (DI setup), `Middlewares`, `Hubs` (SignalR) |
| `Application` | Interfaces (`Interfaces/<Module>`) and view models (`VMs/<Module>`), the API contract |
| `Domian` | EF entities (`Entities/<Module>`) |
| `Infrastructure` | `AppDbContext`, migrations, services (`Services/<Module>`), seeding |
| `CommonOperations` | Constants (roles, error codes), JWT and email helpers, password hashing |

Every endpoint returns a `ResponseVM`: `{ responseCode, code, responseMessage, data, errors }`. `responseCode` is also the HTTP status, and `code` is a stable error key such as `email_taken` or `tests_failed`, which the frontend uses to decide what to show.

## API overview

| Area | Routes |
|---|---|
| Auth | `/api/auth/register, login, google-login, verify-email, resend-verification, forgot-password, reset-password, refresh-token, change-password, logout`, `GET /api/me` |
| Onboarding | `POST /api/onboarding` |
| Learning | `GET /api/courses`, `/api/learning-paths`, `/api/courses/{id}/syllabus`, `/api/courses/{id}/journey`, `GET/POST /api/me/courses[/{id}/start]`, `POST /api/journey/chests/{nodeId}/claim` |
| Lessons | `GET /api/lessons/{id}`, `POST .../run`, `POST .../complete`, `GET .../submissions` |
| Practice | `/api/interview/problems[/topics]`, `/api/interview/today`, `/api/interview/progress`, `PUT /api/interview/date`, `/api/challenges`, `/api/problems/{id}[/run, /submit, /submissions]` |
| Gamification | `/api/me/stats`, `PUT /api/me/daily-goal`, `/api/me/streak`, `/api/me/missions`, `/api/leaderboard/{league or friends}` |
| Store and profile | `/api/store/{tab}`, `POST /api/store/items/{id}/equip`, `GET/PUT /api/me/profile`, `PUT /api/me/profile/showcase`, `PUT /api/me/profile/avatar`, `/api/me/collection`, `/api/me/levels` |
| Social | `/api/users/search?q=`, `/api/users/{username}`, `POST/DELETE /api/users/{username}/follow` |
| AI tutor | `POST /api/tutor/chat` |
| Real-time | SignalR hub `/hubs/live` sends `LeaderboardUpdated` |
| Admin (role Admin) | `/api/admin/dashboard`, `users`, `courses`, `sections`, `chapters`, `nodes`, `problems`, `cosmetics`, and `PATCH /api/admin/{type}/{id}/status` to publish or archive |

## Rules that keep learners safe while admins edit

- New content starts as a draft (`IsPublished = false`). Learners only see published, non-archived content.
- Nothing is hard-deleted. Archive content instead, so learner progress and submissions stay valid.
- Public keys (course slug, node key, problem key) cannot change once learners have used them.
- Solutions of practice problems and hidden tests are never sent to learners. Hidden tests count towards passing but only show up as a count.

## Temporary pieces

- **Code runner:** `MiniCodeRunner` runs beginner Python and JavaScript lessons. `TemporaryProblemJudge` only checks whether the starter code was edited. Both sit behind `ICodeRunner` and `IProblemJudge` and get replaced by Judge0 (Docker) without any API change.
- **AI tutor:** answers with Claude when `AI:ApiKey` is set (model `AI:Model`, default `claude-opus-5-5`), otherwise with built-in rule-based hints.
- **Leaderboard:** there is one weekly "Starter League". Tiered leagues can be added later without changing the response.
