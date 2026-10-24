**Before anything else, read your memory: `MEMORY.md` and every memory file it lists. Follow it in every reply.**

# Zapper

## Rules

1. **Nothing lives only in a chat.** If a decision is not in its source of truth,
   it did not happen.

2. **Architecture is never decided ad hoc.** If the work reveals the architecture
   is wrong, stop and put it to Francois under rule 6. His ruling is written into
   the standard, skill or file it changes before the work carries on.

3. **Only this repository's skills are used.** They are `create-solution`,
   `create-project`, `create-plan` and `feature-execution`, each in its folder in
   `Claude/Skills/`. No other skill builds or changes this repository.
   `feature-review` is not written. Until it is, every check a standard gives to
   it is made by Francois, by hand, when he reviews the pull request. Ruled by
   Francois on 2026-10-22.

4. **Never use em dashes.** Not in code, not in documentation, not in commit
   messages. Use a comma, a colon, a semicolon, or two sentences. Hyphens follow
   normal English, plus this process's own compounds such as `UI-heavy` and
   `tech-only`.

5. **Write plain English.** English is a second language here. Short sentences
   and ordinary words. Never use a metaphor as if it were a real term: write
   "the list of accepted vulnerabilities", not "the ledger". Prefer the words this
   process already has, such as Feature, standard and contract. An idea with
   no name gets described rather than coined. This applies to chat as much as to
   documents.

6. **A question is asked in conversation, never as a multiple-choice tool.** A
   decision that needs Francois's ruling is put to him as plain text: what was
   found, a recommendation, then the question itself. His reply in the
   conversation is the ruling.

## Sources of truth

There are no tickets. Everything is in the repository.

- **Plans** → `ai/Plans/<Plan_Title>.md`. There is no ADR, BRS or tech spec:
  what they held is in the plan.
- **Contracts** → `Docs/Contracts/<Api>/<version>/<route>/`. A contract
  approved in a plan waits in
  `ai/Plans/<Plan_Title>/Contracts/<Api>/<version>/<route>/` until
  `feature-execution` moves it to `Docs/Contracts/` in its Feature's first
  increment, so `dev` never holds a contract without its code. Ruled by
  Francois on 2026-10-22.
- **Code** → `Code/`.
- **Process** → this file, `Claude/Skills/` and `Claude/Standards/`.

## Vocabulary

- **Feature**: one unit of work. Owns its contracts. Its title is given in plain
  words and converted as `Claude/Standards/Branching.md` describes, for example
  `SMTP_Failover`.
- **Plan**: one piece of work, written down by `create-plan` before it is built.
  It holds the decisions that affect more than one Feature, the list of
  Features, and one section per Feature: its rules, its scenarios, how it is
  built, its contracts and its implementation order. "A Feature's plan" means
  that Feature's section. Every Feature in a plan is tech-only.
- **Decision**: a decision in a plan, `<Plan_Title>` D-<n>, that affects more
  than one Feature. It binds every later plan until a later plan replaces it by
  name.
- **Contract**: a frozen, versioned **API surface**: one operation on one route
  of one API. Nothing else is a contract. Interfaces are proved by unit tests
  and queue messages by integration tests, so neither is frozen.
- **Frozen**: changeable only by a new version, once the contract is under
  `Docs/Contracts/`. A contract waiting under
  `ai/Plans/<Plan_Title>/Contracts/` is changed in place by a run of
  `create-plan` when Francois asks, and its `Status` takes the date of that
  approval. Ruled by Francois on 2026-10-24.

## Where things live

```
/CLAUDE.md
/compose.yml            ← the local database, the Migrator, every API and the Acme stub, run with `docker compose up -d`
/AcmeStub/              ← a stand-in for the Acme Pay gateway, outside Code/ and the solution; follows the brief's Appendix A, not the standards
/Claude/
    Skills/<skill>/     ← one folder per skill; the authority for that skill's work
    Standards/          ← how the work is built
    Templates/          ← Contract, Plan
/ai/
    Plans/<Plan_Title>.md ← one plan, presented to Zapper
    Plans/<Plan_Title>/Contracts/<Api>/<version>/<route>/ ← its contracts, until their Feature is built
/Code/                  ← the solution and its projects
    .dockerignore       ← keeps bin/, obj/ and Certificates/ out of every image
    Zapper.<Host>/Dockerfile ← the image of one host: the Migrator, or one API
    Certificates/       ← this machine's database certificates; only .gitignore is committed
/Docs/
    Contracts/<Api>/<version>/<route>/
        <method>.openapi.json   ← the frozen fragment
        <method>.md             ← the document that explains it
```

The contract path follows the API route, so the API is the first segment and the
endpoint is the rest. Nothing is chosen, everything is derived.
