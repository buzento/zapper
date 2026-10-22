# Branching Standards

The branch management standard for the solution. It fixes which branches exist,
what a branch is called, what may be committed to it, when it is pushed, and
where its pull request goes, so that a feature can be executed without inventing
any of them.

Every skill that creates a branch, `create-solution`, `create-project`,
`create-plan` and `feature-execution`, reads this before creating it and follows
it for every commit, for the pull request and for what happens after the merge.
`feature-review` checks against it. It defines three names, and no skill and no
template restates them.

It says where a change goes, not what happens to it once it is there. It stops
at the pull request, except for deleting the local branch once a person has
merged it.

Every rule here has a named enforcer in section 8, including the rules whose
enforcer is nothing.

# 1. The branches

Three long-lived branches, and nothing between them:

```
main  ←  test  ←  dev  ←  feature branches
```

| Branch | Position |
|---|---|
| `dev` | feature branches are cut from it and their pull requests target it |
| `test` | promoted from `dev` |
| `main` | production, promoted from `test` |

**No commits to `dev`, `test` or `main`.** Every change reaches `dev` as a pull
request from a feature branch. There is no exception.

**Nothing merges into `main` without approval. Ever.** This is enforced by branch
protection on the server as far as the platform allows, and stated here so the
intent survives a platform that allows less than it should.

**`dev` is used for exactly two things**: it is the point a feature branch is cut
from, and it is the target of that branch's pull request. It is never a branch
anyone stands on to do work.

# 2. The naming scheme

Three names share one scheme, so learning it once is enough:

```
Feature/SMTP_Failover      the branch
SMTP_Failover_3/7          an increment commit subject
SMTP_Failover              the pull request title
```

The scheme:

- **Underscores separate the parts, and the words inside the feature title.**
- **Each word of the feature title starts with a capital letter.** Every letter
  after the first is left exactly as the title was given.

**The feature title is asked for**, in plain words, by the skill that starts the
work, every run, before anything is written. It says what the work is, in a few
words, the way a reader of the branch list would want to see it.

**`feature-execution` does not ask for it.** It takes the title from the
Feature's `# Feature:` heading in the plan, where Francois gave it when the plan
was made. Ruled by Francois on 2026-10-22.

Building the feature title part, which is the only part with any work in it:

1. Take the feature title as it was given.
2. Capitalise the first letter of each word.
3. Leave every other letter as it was given.
4. Replace each space with an underscore.
5. Drop anything that is not a letter, a digit or an underscore.

`SMTP failover` gives `SMTP_Failover`. `Employee document upload` gives
`Employee_Document_Upload`.

**Step 3 is why there is no list of abbreviations to maintain.** A title given as
`SMTP failover` already carries `SMTP` in capitals, so leaving the other letters
alone preserves it. It also fails where it can be seen: a title given as
"smtp failover" produces `Smtp_Failover`, which is wrong at a glance rather than
wrong silently.

**Step 5 exists because git rejects some characters outright**, a colon among
them, and tolerates others that read badly in a branch name.

Nothing in these names is chosen beyond the title. All three are derived from it.

# 3. The feature branch

**Cut from `dev`**, from the current state of `dev` on the server.

**Created by the skill that does the work**, locally, as one of its first steps
and before anything is written: `create-solution`, `create-project`,
`create-plan` or `feature-execution`. Never created by hand in advance. Ruled by
Francois on 2026-10-22.

**Created if missing, checked out if present.** A feature spans several sessions,
so on every session after the first the branch already exists. A skill that only
knows how to create it fails on the second session, which is every session after
the first.

**One branch per feature.** Not one per increment.

A feature branch may live as long as the feature takes. There is one developer
and one feature at a time, so the usual objection to a long-lived branch, painful
merges caused by parallel work, does not apply. **That reasoning expires the day a
second developer starts**, and nothing will notice on its own.

# 4. Commits

**One commit per increment.** The increment is the commit. When the increment is
finished, it is committed; nothing is committed before that.

**No checkpoint commits.** No `wip`, no `fix typo`, nothing between increments.
Increments are small enough that a checkpoint is not needed.

**Every commit on a feature branch carries the increment subject format:**

```
SMTP_Failover_3/7
```

The last part is the increment number and the total, taken from the
implementation order in the feature's plan. Work with no plan, which is
everything `create-solution`, `create-project` or `create-plan` does, is one
increment, so its one commit's last part is `1/1`. A commit whose subject does not match this
format **is an error**, not something to skip over. That is the whole reason
checkpoint commits are banned: the rule can be checked strictly, and strict
checking fails loudly where tolerant checking fails silently.

**The commits are the record of which increments are finished.** Listing
`git log dev..HEAD` and taking the highest increment number gives the state of
the feature, locally and with no network. This holds only while increments are
worked in order, which is what the implementation order is for.

# 5. Pushing

**The branch is pushed after each increment commit.**

**Nothing builds on a feature branch.** The push is a backup and nothing else.
The whole suite runs once, on the feature as a unit, when the pull request is
opened.

Those two rules have different reasons and both have to be stated. Read alone,
"pushed after each increment" invites a build that nobody wants, and the CI
configuration is the only thing preventing it.

The branch first appears on the server after increment 1 is committed, since
there is nothing to push before that.

**A pushed branch is not evidence that anything is sound.** The push happens at
the commit, before any whole-feature check has run, so the server holds work in
progress and sometimes work that will turn out to be wrong.

# 6. The pull request

**One pull request per feature**, opened after the last increment, by the skill
that did the work. Ruled by Francois on 2026-10-22.

**From the feature branch into `dev`.** Never into `test`, never into `main`.

**The title is the feature title:**

```
SMTP_Failover
```

**The body is the skill's closing report**, so a reviewer reads what was done
and what is left without opening the conversation.

## How a skill ends its run

Every skill that creates a branch ends its run with these commands, from the
repository root, in this order, with `<FeatureTitle>` built by section 2.

**First, check that every change is this run's.** List them:

```bash
git status --short
```

Every line must name a file this run wrote, changed or deleted. **Where a line
names anything else, stop and ask Francois** rather than commit it, because
`git add -A` below takes every line.

**Then commit, push and open the pull request:**

```bash
git add -A
git commit -m "<FeatureTitle>_<n>/<total>"
git push -u origin Feature/<FeatureTitle>
gh pr create --base dev --head Feature/<FeatureTitle> --title "<FeatureTitle>" --body-file - <<'EOF'
<the closing report>
EOF
```

`feature-execution` commits and pushes after each increment, by sections 4 and 5,
and opens the pull request after the last one. Every other skill has one
increment, so it runs all four together. `gh pr create` prints the pull
request's address: give it to Francois. **Then wait.** The run is not over until
the pull request is merged.

## After the merge

Francois merges the pull request himself, then says so **in the same
conversation**. Only then, and only for the branch that conversation created,
delete the local feature branch:

```bash
gh pr view Feature/<FeatureTitle> --json state --jq .state
```

**Expect `MERGED`.** Anything else means the pull request is not merged: stop and
say what it printed. Then:

```bash
git fetch origin dev
git switch dev
git merge --ff-only origin/dev
git branch -D Feature/<FeatureTitle>
```

`git switch dev` creates the local `dev` from the server's where it does not
exist yet. **`-D` and not `-d`**, because a pull request merged by squashing
leaves the branch's own commits outside `dev`, so `-d` refuses a branch that is
merged. The `gh` check above is what makes `-D` safe. The branch on the server is
deleted by the person who merged it.

Standing on `dev` here is not work on it: nothing is written or committed there,
and the next run creates its own feature branch.

# 7. What is never done by an agent

Stated as prohibitions rather than left implied. A skill told only what to do
next knows what comes next; it does not know what it may never do.

- **Never commit to `dev`, `test` or `main`.**
- **Never merge anything.**
- **Never delete a branch**, except the local feature branch the same
  conversation created, once its pull request is merged, by section 6, "After the
  merge".
- **Never open a pull request into `test` or `main`.**

# 8. Enforcers

Every rule above appears here with the thing that catches it. Where that thing is
nothing, the row says nothing, because a column that claims coverage it does not
have is worse than no column.

| Rule | Enforcer |
|---|---|
| No commits to `dev`, `test` or `main` | Branch protection on the server, as far as the platform allows |
| Nothing merges into `main` without approval | Branch protection on the server |
| Nothing builds on a feature branch | The CI configuration, which is not part of this document |
| A commit subject that does not match the format | Visible immediately in `git log dev..HEAD`, and `feature-review` |
| One commit per increment, no checkpoint commits | `feature-review` |
| The branch name is built from the feature title | `feature-review` |
| The pull request title is built from the feature title | `feature-review` |
| The pull request targets `dev` | `feature-review` |
| The agent never merges, and deletes only its own local feature branch after the merge | Nothing. A written rule only |
| A local feature branch is deleted only once its pull request is merged | The `gh pr view` check in section 6, "After the merge" |
| A skill commits only this run's changes | The `git status --short` check in section 6, "How a skill ends its run" |
| The branch is pushed after each increment commit | Nothing |
| Increments are worked in order, which the read-back depends on | Nothing. The implementation order is frozen at a planning gate, and nothing in git checks that it was followed |
| The reasoning that permits a long-lived branch expires with a second developer | Nothing |

# 9. Not in scope

Each of these is something a reader of this document could otherwise believe it
governs.

**Merging.** Strategy, timing, and who does it. A person performs the merge after
reviewing the pull request, and deletes the branch on the server afterwards.
Section 6 states only what the skill does once told that the merge happened.

**What builds, runs or deploys on each branch.** CI and deployment.

**The implementation order.** That belongs to planning. This document defines
only the format that carries the increment number, not what the numbers are or
how they are decided.

# 10. Considered and rejected

- **One pull request per increment.** The original model, in which the increment
  number rode in a pull request title. Rejected because it means a server build
  per increment, which does not hold up over a real feature, and because it
  splits one feature's review across five or seven pull requests.
- **Merging each increment into `dev` or `main` as it lands.** Increment 1 is
  always the failing acceptance tests, so a shared branch would be red by design
  for the whole feature. A branch that is permanently red cannot report anything,
  and the difference between "not finished" and "broken" disappears.
- **Quarantining the in-flight acceptance tests behind a filter** so increments
  could merge to a shared branch anyway. It is an opt-out with nothing watching
  it, and the feature branch does the same job with no new machinery.
- **Not pushing the feature branch until the feature is done.** It bundled two
  separate things. The concern was server builds, which the CI configuration
  handles on its own, and once it does the push costs nothing and buys a backup
  of work that would otherwise exist on one disk for weeks.
- **Checkpoint commits, squashed at the increment boundary.** Squashing is nearly
  free, but it was the only rule here that depended on someone remembering to do
  something before moving on. Small increments remove the need instead.
- **Keeping the increment commits on `dev` with a merge commit.** It would let a
  line of code trace back to a row in the implementation order. Rejected because
  merging is manual and out of scope, and because the squashed commit still
  carries the feature title, which is the link that matters. What is lost
  is only the ability to look backward at finished work.
