# Organization registration

## API and account lifecycle

`POST /api/auth/register` accepts the existing personal-registration fields plus:

| Field | Meaning |
| --- | --- |
| `accountType` | `Donor` (default), `CharityOrganization`, `RecyclingOrganization`, or `DisposalOrganization` |
| `fullName` | Person's name for donors; organization's display name for organizations (2–100 characters) |
| `representativeName` | Organization representative (2–100 characters; required for organizations) |
| `taxCode` | Tax/organization registration number (1–50 characters; required for organizations) |
| `certificateImageUrl` | Uploaded Supabase certificate image URL; required for organizations |

Organization address is required (5–500 characters). Existing username, email,
Vietnamese mobile number, duplicate-account and password rules apply. Public
registration accepts only the four account types above.

Registration returns the existing `{ userId, message }` response. Accounts start
in `PendingVerification` and become active through the existing email OTP flow
(`verify-registration` and `resend-verification`). Login uses the existing role
claims and frontend routes. Organization names continue to use `Users.FullName`,
which existing distribution and processing queries already use.

`GET /api/auth/me` and the manager account list include `representativeName`,
`taxCode`, and `certificateImageUrl`. Managers can open the certificate from the
account list after email activation.

## Storage

The frontend reuses `uploadImages` with folder `organization-certificates` in the
existing Supabase bucket. It checks MIME type, file signature and size (5 MB
maximum; JPEG, PNG, WebP), normalizes extensions, previews the file and retains
the uploaded URL when registration needs to be retried.

Backend `Supabase:Url` and `Supabase:Bucket` must match frontend
`VITE_SUPABASE_URL` and `VITE_SUPABASE_BUCKET`. The backend permits only HTTPS
certificate URLs in that configured storage folder. The image upload uses the
existing frontend `VITE_SUPABASE_ANON_KEY` and bucket policies.

## Database and verification

Migration `20260915143640_AddOrganizationRegistration` adds three nullable User
columns. It creates the disposal role only when no role with that name exists,
preserving existing role IDs and assignments. The role is retained on migration
rollback because existing accounts may reference it.

Apply before starting the updated API:

```powershell
dotnet ef database update --project src/DAL --startup-project src/Capstone-API
```

Automated service checks use an isolated LocalDB database and a fake email sender:

```powershell
dotnet run --project tests/ProcessingOperations.Checks --configuration Release
```

Checks cover all four registration types, rejected internal roles, required
organization fields, certificate URL validation, duplicate registration, OTP
activation, login roles, inactive accounts and manager/profile metadata. Browser
checks covered all four forms, image validation, upload/request payloads and OTP
screen transitions with mocked registration/storage responses; Supabase upload
and public image retrieval were additionally tested against the configured bucket.

The Supabase test image is a synthetic one-pixel PNG. Anonymous deletion returned
no deleted objects; its path is recorded in the frontend workspace at
`.codex-build/supabase-test-object.txt` for removal through the storage dashboard.
