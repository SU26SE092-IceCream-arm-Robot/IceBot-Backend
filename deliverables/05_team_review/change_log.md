# Documentation Change Log — IceBot Backend Deliverables

**Purpose**: Record future changes to the deliverable baseline and their evidence impact. Add the newest entry at the top of the log. One entry may cover several files only when they implement one coherent correction or decision. An entry may be marked approved only by the decision authority defined in `review_guide.md`; the author cannot independently approve a material change.

## Initial Baseline

The existing Project Introduction, SRS, RTM, UML, database-design documents, and team-review package form the **working documentation baseline**, not an approved final baseline. Before formal review begins, record its branch, commit/hash or named workspace snapshot, date, included-file manifest, and known uncommitted/untracked files here.

| Field | Value |
|---|---|
| Baseline ID | `BASELINE-001` (provisional) |
| Branch |  |
| Commit/hash or workspace snapshot |  |
| Baseline date |  |
| Included file manifest |  |
| Known uncommitted/untracked files |  |
| Recorded by |  |

## Entry Template

### Date

`YYYY-MM-DD`

### Identity and revision

- Change ID: `DOC-YYYY-NNN`
- Linked issue/open-question ID:
- Entry status: Proposed / Accepted / Rejected / Deferred / Superseded / Applied / Verified
- Baseline before:
- Baseline after:

### Changed files

- `deliverables/<path>.md` — section/requirement/question changed

### Reason for change

- Change type: factual correction / evidence update / confirmed team decision / terminology alignment / traceability repair / presentation-only change
- Previous claim or issue:
- New claim or resolution:
- Related question or requirement ID:

### Reviewer

- Author:
- Reviewer/decision owner:
- Decision authority:
- Approval date:
- Review status: Proposed / Conditional / Approved / Rejected

### Evidence updated?

- Yes / No / Not applicable
- Evidence source or decision record:
- Does `00_repo_evidence/` require a separate authorized update? Yes / No

### Verification performed

- Mechanical checks and result:
- Technical checks and result:
- Independent reviewer:
- Verification date:
- All affected downstream files checked? Yes / No — list:

### Follow-up needed?

- Yes / No
- Affected downstream files:
- Remaining uncertainty label or open-question ID:
- Owner and target date:

### Supersession / rollback

- Supersedes change ID:
- Superseded by change ID:
- Reversal/rollback guidance, if applicable:
- Exact decision record:

---

## Change Entries

### 2026-08-08 — Team review package corrective pass

- **Change ID**: `DOC-2026-001`
- **Status**: Applied; independent verification pending
- **Baseline before/after**: working tree snapshot; formal commit/hash baseline not yet recorded
- **Changed files**: `review_guide.md`, `open_questions.md`, `change_log.md`, `team_review_checklist.md`
- **Reason**: Apply necessary findings from `codex_review_team_review_package.md`: correct RTM terminology, add baseline/triage/authority/verification mechanics, add evidence-derived high-impact questions, reframe speculative items, add specialist roles, and strengthen sign-off/change tracking.
- **Reviewer/decision authority**: independent team approval pending
- **Evidence updated?** No; no `00_repo_evidence/` file was modified.
- **Verification**: scoped file/diff, terminology, heading, reference, and whitespace checks required after editing; technical/runtime verification not applicable to these procedure-only changes.
- **Follow-up**: record a formal baseline; assign owners and triage metadata; obtain independent review and required sign-offs.
