# Security Standards

The security standard for the product. It states what must be true of the software, its
configuration, and how it is built and deployed, so that the product meets its targets and
no code change is needed when compliance is pursued. It states what must be true, and never
how it is built.

Planning reads this before the architecture is decided and before any feature is planned,
and plans against it. The plan and
the contracts are written against it. `feature-execution` reads it before
writing an increment. `feature-review`, and the person who reviews the increment, check the
increment against it.

Every rule in this document is settled. Each one was argued and decided before it was
written here, and none of them is re-opened during execution. Where this document states
one way to do something, that is the only way, and an equivalent is not substituted for it.

Every rule has a named enforcer in section 15, including the rules whose enforcer is
nothing.

# 1. Scope

This document covers the product: its software, its configuration, and how it is built and
deployed.

**The targets.**

- **POPIA**, the Protection of Personal Information Act.
- **ASVS 5.0.0**, OWASP's Application Security Verification Standard, at Level 2, for the
  whole product. Level 2 includes Level 1.
- **SOC 2**, the 2017 Trust Services Criteria, Security category, being the common
  criteria, with the 2022 points of focus.

PCI DSS is not a target. Section 2 states the rule that takes its place.

**A requirement is in this document when meeting it would ever need a change to code,
configuration, or the build and deployment pipeline.** Everything an auditor would check by
looking at the product is in. Nothing an auditor checks by looking at the organisation is.

- **In:** all of ASVS 5.0.0 Level 2; SOC 2's logical access (CC6), system operations (CC7)
  and change management (CC8), with the build and deployment pipeline; POPIA sections 14,
  19, 22, 23, 24, 26 and 27, where meeting them needs the product; and hosting where the
  software depends on it, such as the vault, database roles and encryption in transit.
- **Out:** SOC 2's control environment, communication, risk assessment, monitoring of
  controls and supplier management (CC1 to CC5, and CC9); physical access, which CC6 also
  holds; POPIA's Information Officer, the PAIA manual, the wording of privacy notices, and
  agreements with operators; physical security, staff screening and training, and access
  reviews done by people.

**The product is built so that it would pass a penetration test, and that asks nothing
beyond ASVS 5.0.0 Level 2 and the line above.**

| Subject | Where it is stated |
|---|---|
| How code is written, including a security rule that constrains it | `BackendCode.md` and `FrontendCode.md` |
| Tests, including the authorization and data protection tests | `Testing.md` |
| Branches, commits and the pull request | `Branching.md` |
| How a decision that affects more than one feature is built | a decision in a plan |
| What one feature must do | the rules and scenarios in its plan |
| How one feature is built | its plan |

**A security rule that constrains how code is written is the code standards'.** Everything
else about security is this document's.

**Where a rule here has a code half, a pointer follows the rule.** It names the code
standard, its section, and in a few words what that section holds. Neither document repeats
the other's rule. Where the words "to be set", in brackets, stand in place of a section, the
rule belongs to that standard, and the section that states it is not yet named here.

**Every value in this document is a floor. A customer may be given a stricter setting, and
never a weaker one.**

**No rule in this document is sold as an upgrade. Every customer gets the floor. A control
stronger than the floor may be a paid feature.**

# 2. What is sensitive, and how personal information is kept

## Payment card numbers

**No payment card number reaches the product: not an API, a database, a log, a frontend or
a test.** This rule changes only by a decision in a plan that sets out to store, process or
passing on card data. PCI DSS 4.0.1 is then a target for the part that handles card data.

## The protection levels

**Data about a person is at one of two protection levels: sensitive data and personal
information.** A rule about data at either protection level reaches both alike. Where it is
not certain which level a value is at, it is sensitive. A value keeps its level however it
is written, including when it is encoded or carried in a token. Data about no person is at
no level, and neither is the key of a record.

### Sensitive data

Sensitive data is every category below:

- pay and salary;
- bank details;
- tax and identity numbers;
- date of birth;
- home address;
- disciplinary records;
- religious or philosophical beliefs;
- race or ethnic origin;
- trade union membership;
- political persuasion;
- health;
- sex life;
- biometric information;
- criminal behaviour as section 26 of POPIA describes it;
- performance data, being reviews, ratings and assessments against goals;
- grievances and complaints, whether made by an employee or about them;
- investigations, including reports made by a whistleblower;
- why a person left, being the reason for a resignation, a dismissal or a redundancy;
- recruitment assessments, being interview notes, test results, and reference and
  background checks;
- deductions a court orders from pay;
- and benefits and the dependants on them, such as medical aid and pension.

Special personal information, being the categories section 26 of POPIA names, is sensitive
data.

### Personal information

Personal information is every other piece of information about an identifiable person. The
values below are examples of personal information, named to show what the definition
reaches:

- a name;
- an email address;
- a phone number;
- an employee number;
- a job title.

## Where data at either level never goes

**Data at either protection level is never put in a URL, in its path or in its query
string.**

**A secret is never put in a URL, in its path or in its query string.** Section 3 states
what a secret is.

**Data at either protection level is never kept by a cache on its way to the browser, such
as a load balancer or a proxy, or by a cache inside the product.**

**Data at either protection level is never sent to a service that tracks people or measures
how the product is used.**

**A frontend's published settings file holds no data at either protection level.** That no
secret is ever put in a frontend is `FrontendCode.md` section 7's.

## What a browser keeps

**A response that carries data at either protection level tells the browser not to keep
it.** The header an API writes for it is `BackendCode.md` section 7's.

**Data at either protection level is never kept in the browser's storage or in a cookie. A
token is kept only as this document's rules for tokens state.** What a frontend keeps in the
browser is `FrontendCode.md` section 10's.

**When a session ends, a frontend removes everything it received while that session was
signed in, from memory and from anything the browser keeps, including when the server
cannot be reached.** How a frontend empties its queries and its stores is `FrontendCode.md`
section 10's.

## Encryption and backups

**Data at either protection level is stored encrypted in each column its feature's plan
names.**

**Every document uploaded to the product is encrypted before it is stored.**

**The database is backed up daily.**

**Every backup is encrypted.**

## How long a record is kept

**Every record of personal information has a stated period for which it is kept, and is not
kept beyond it.**

**A record kept beyond its period for historical, statistical or research purposes is used
for nothing else.**

**A record used to make a decision about a person is kept at least as long as a law or code
of conduct requires, or where none does, long enough for that person to ask for it.**

## Removing personal information

**When a record may no longer be kept, or its deletion is approved, the product
de-identifies it within one day: every value that identifies the person is overwritten with
a fixed value, and the record is marked deleted.** No row holding data at either protection
level is deleted. An uploaded document is deleted from storage at the same point.

**A value that replaces an identifying value cannot be turned back into it, and does not
identify the person on its own or together with other values the product keeps.** No backup
is kept longer than 30 days. After a backup is restored, every de-identification and every
document deletion made since is carried out again before the data is used.

## Restricting, correcting and producing personal information

**A record of personal information can be restricted.** A restricted record is kept, and is
used only as proof, with the person's consent, to protect the rights of another person, or
in the public interest. A restriction is lifted only after the person has been told.

**Every piece of personal information the product holds can be corrected, or de-identified
as this document states, except where a rule of its feature requires it kept.**

**Where a correction is asked for and not made, a note saying so can be attached to the
information, and the product shows the note wherever it shows the information.**

**For any person, the product can produce every piece of personal information it holds
about them, in a form that person can read, together with the third parties that have been
given access to it.**

## Credentials for infrastructure

**A piece of infrastructure, such as a database server, is given a credential only after it
is registered and authorised, and that credential stops working when the infrastructure is
no longer in use.**

# 3. Secrets and cryptography

## What a secret is

**A secret is any value that proves who its holder is, gives access to a system or to data,
or makes, reads or checks protected data, whether a person or the product holds it.**

The values below are examples of secrets. Each is named to show what the definition
reaches, and none is a name to look for: a password, an authentication token, a refresh
token, a database password, a key another API uses to call ours, a customer's secret, a
signing key, a tenant's encryption key, the backups' key, and a connection string that
holds a password.

## Where a secret is kept

**A secret the product itself uses is kept in the vault. Where there is one for each tenant
or for each user, it may instead be stored encrypted by a key that is kept in the vault.**

**No secret is in the repository or in anything a build produces.** That no secret appears
in a settings file is `BackendCode.md` section 14's. That no secret is ever put in a
frontend, and that no `.env` file is committed, is `FrontendCode.md` section 7's. That no
secret is written into back end source code is `BackendCode.md` section 14's.

## Who and what may read a secret

**Each part of the product reads only the secrets it uses, under an identity of its own.**

**In a deployed environment, only the people named as that environment's administrators can
reach its vault, and every read and every change is recorded.**

**A developer's machine never holds a secret of a deployed environment.**

## Keys

**A shared secret key is held by no more than two parties, and a private key by one.**

**Every key is made by a cryptographically secure random number generator or by an approved
key derivation function, kept and read as this document's rules on the vault state,
replaced as its rules on replacement state, and destroyed once nothing remains encrypted,
signed or checked with it.**

## When a key or a secret is replaced

**Every secret kept in the vault is replaced at least once every 12 months, except a
customer's secret, which the customer replaces when they choose.**

**A secret stored encrypted by a key kept in the vault is replaced when it may have been
exposed.**

**Any secret is replaced at once when it may have been exposed, or when a person who could
reach it stops being one of that environment's named administrators.**

## The cryptographic inventory

**A list of every key, algorithm and certificate the product uses is kept in
`Docs/CryptographicInventory.md`. It states for each key where it may and may not be used,
and what data it may and may not protect, and it is changed in the same increment as what it
lists.**

## Algorithms and strength

**Every algorithm, key length and setting used for cryptography can be changed at any time.
Every key and every secret can be replaced, and the data a key protects can be encrypted
again, without losing any of it.**

**Every cryptographic mechanism gives at least 128 bits of security, counting its algorithm,
its key size and its settings.**

**Cryptography uses only a mechanism that Appendix C of ASVS 5.0.0 marks as approved, with
the parameters Appendix C requires for it. A mechanism it marks as legacy or disallowed is
never used.**

**Encrypted data is protected against being changed, by authenticated encryption or by a MAC
over the encrypted data.**

**A password is stored only as a hash made by a password hashing function.**

**A hash used in a signature, or to prove that data has not changed, has at least 256 bits of
output where finding two inputs with the same hash must be impossible, and at least 128 bits
where only finding a second input for a given hash must be impossible.**

**A key made from a password is made only by a password-based key derivation function.**

**A key is never generated in a way known to make it weak, such as an RSA key open to Fermat
factorisation.**

## Random values

**A random value that must not be guessed is made by a cryptographically secure random number
generator and carries at least 128 bits of entropy.** A lookup secret, an out-of-band code
and a time-based one-time password follow their own rules in this document instead. A UUID
is never used as a value that must not be guessed.

# 4. Signing in

## The ways to sign in

**This document lists every way a caller signs in, and states for each one what proves the
caller and how strongly.**

| Caller | What proves it | How strongly |
|---|---|---|
| A person, through the product's own sign-in | The password and a second factor of the user they sign in as | Two or more factors |
| A person, through their customer's identity provider | The provider's signed assertion, and a second factor the product asks for where the provider does not prove that two were used | Two or more factors |
| One of the product's services calling another | A service account, whose password is kept in the vault | A credential generated at random, long, and never typed by a person |
| One of the product's APIs calling another | A secret key of its own, kept in the vault | A credential generated at random, long, and never typed by a person |
| A customer's system | A customer secret, whose copy the product keeps in the vault, and then an authentication token | A credential generated at random, long, and never typed by a person |

**No caller signs in by a way this document does not list, and each way enforces the
controls and the strength listed with it.**

## Factors and one-time codes

**Signing in as a user needs two or more factors, whether one mechanism holds them or
several do.**

**A new user sets up a second factor before it reaches anything.**

**A lookup secret, an out-of-band code and a time-based one-time password each work once.**

**A lookup secret with less than 112 bits of entropy is stored only as a hash made by a
password hashing algorithm with a random salt of at least 32 bits. One with 112 bits or
more may be stored as an ordinary hash.**

**A lookup secret, an out-of-band code and the seed of a time-based one-time password are
made by a cryptographically secure random number generator.**

**A lookup secret and an out-of-band code each carry at least 20 bits of entropy.**

**An out-of-band request, code or token lives at most 10 minutes. A time-based one-time
password lives at most 30 seconds.**

**A one-time password sent by text message or phone call is offered only for a phone
number already confirmed, only beside a stronger method, and only with its risks explained
to the person.**

**An out-of-band request, code or token works only for the sign-in it was made for.**

## Passwords

**This document names the kinds of words a password may not contain: the product's own
names, the name of the tenant and of the company the user belongs to, the department and
role names that company holds, the person's own name, and the user's sign-in name.**

**A password is refused when it contains, ignoring case, a word of any kind this document
names.**

**A password is at least 12 characters long.**

**The longest password accepted is at least 64 characters.**

**A password may hold any characters. No rule requires upper case, lower case, digits or
symbols.**

**A password is checked exactly as the person entered it, with nothing cut off and no change
of case.**

**A password set at registration or on a change is refused when it is among at least the
3,000 most common passwords that meet the other password rules.**

**A password set at registration or on a change is refused when it appears in a set of
passwords known from breaches.**

**A password stays valid until it is found to be compromised or the person changes it. No
password expires on a schedule.**

**A person can change the password of any user they hold.**

**Changing a password requires the current password and the new one.**

**No password hint and no secret question exists.**

## Failed sign-in attempts

**Five failed sign-in attempts within 15 minutes lock the user for 15 minutes. An attempt
fails when either the password or the second factor is wrong.**

**Failed attempts across many users are detected, not limited at sign-in.**

**A system caller is never locked by failed attempts.**

**A system caller's identifier and secret are checked together, and a failure never shows
which of the two was wrong.**

**A customer may set a lock that lasts until it is released. A person can always release
their own lock by resetting their password, and the reset needs their second factor.**

**A failed sign-in never shows whether the sign-in name or the password was wrong. A
sign-in name that has no user behaves exactly like one that has, including being locked.**

## Invitations and recovery

**An invitation or activation code is random, works once, and expires 72 hours after it is
sent. Sending a new one cancels the old one. The product never creates a password for a
person.**

**An invitation or activation code is never sent to a server in a URL. A link that carries
one holds it after the `#`, and the page sends it to the server in the body of a request.**

**A code is used up when the person sets their password, never when the link is opened.**

**A code is made by a cryptographically secure random number generator, holds at least 128
bits, and is stored only as a hash.**

**Resetting a forgotten password never lets a person past a second factor set up on that
user.**

**A person who has lost a factor is given a new one only after their identity is confirmed
as strongly as it was when that user was created.**

## Identity providers

**Where a customer's identity provider signs a user in, this document's floor still applies.
Either the provider proves that two factors were used and says so, or the product asks for a
second factor itself.**

**Where more than one identity provider signs people in, a person signed in by one can never
be taken for a person signed in by another.**

**The signature on every sign-in assertion, such as a token or a SAML assertion, is checked,
and one that is unsigned or wrongly signed is refused.**

**A SAML assertion is accepted once.**

**Where a function needs a stated strength or recentness of sign-in, it is checked against
what the identity provider returned. Where the provider returns nothing, the sign-in is
treated as a single factor.**

## System callers

**A system caller proves which caller it is before it reaches anything, and it is never
taken on trust for being inside our own network.**

**A system caller is registered and authorised before it is given a credential, and that
credential stops working when the caller is no longer in use.**

**A credential that identifies a system caller is generated at random, is long, is kept only
in the vault, and is never typed by a person.**

**A fixed secret identifies a system caller only. It never carries a person's session.**

**An authentication token issued to a system caller is refused once that caller's secret is
withdrawn.**

**A key that signs an authentication token is held only by the product, and never by the
caller the token is issued to.**

## Accounts

**The product holds no account that exists by default, such as one named "admin" or "root".
Every account belongs either to the person it was created for or to the part of the product
that uses it, and never to a person on behalf of a system.** That a migration creates no
account is `BackendCode.md` section 13's, which lets only static data the product owns and
ships into a migration.

**A user reaches a customer's records only where someone that customer authorised has
granted it. A person may register a user themselves, and such a user reaches only the
records of that person.**

**When a user is disabled or deleted, its sign-in name and password stop working.**

# 5. Sessions

## The tokens that carry a session

**An authentication token is the token a caller, a person or a system, presents to an API
after signing in. A session token is any token that carries a person's session, and a
person's authentication token is one. A reference token is a session token that carries
nothing itself, and that the server looks up to find the session it stands for.** A refresh
token is an example of a reference token.

## A session and its user

**A session is for one user. A person who holds more than one user signs in again to reach
another of them, and a session is never widened to a second user.**

**A user may hold at most 5 sessions at once. Signing in beyond that ends the user's oldest
session.**

**No session is created unless the person acts or agrees.**

## How long a session lasts

**A session ends after 30 minutes without activity by the person.**

**A session ends one day after it began, however much it has been used.**

## Identity providers and sessions

**This document lists every system that creates or manages a session for a user, and states
how their lifetimes, their ending, and any need to sign in again, are kept in step.** The
systems that create or manage a session for a user are the product's own identity service
and, where a customer connects one, that customer's identity provider.

**Every session follows this section's lifetimes and ends in every way this section states,
whichever of them signed the user in. A session at an identity provider never keeps a
session of the product alive, and signing in again after a session ends requires the
identity provider to sign the person in again.**

**Sessions between the product and an identity provider end, and ask for signing in again,
as this document states.**

## Session tokens

**Every session token is verified by a back end service, and never by a frontend.**

**A person's session is carried by a token issued when they sign in, and never by a fixed
key or secret.** That no web frontend carries a session by a fixed key is `FrontendCode.md`
section 7's, which puts no secret in a frontend.

**A reference token is unique, is made by a cryptographically secure random number
generator, and carries at least 128 bits of entropy.**

**Signing in, and signing in again, issues a new session token and ends the one held
before.**

## Ending a session

**When a session ends, for any reason, its authentication token is refused from that
moment.**

**Disabling or deleting a user ends every session that user holds.**

**After a factor is changed or removed, including by a password reset, every other session
that user holds ends.**

**Every page and screen that requires signing in has a visible way to sign out.**

**An administrator can end the active sessions of one user, or of every user of the customer
they administer.**

**A person can see the active sessions of the user they are signed in as, and after
confirming at least one factor again, can end any or all of them.**

## Changes that need signing in again

**Changing an email address, a factor, or anything else used to recover an account requires
the person to sign in again in full first.**

# 6. Authorization

## Operations

**An operation's security requirement names the permissions a caller must hold, or none where
any signed-in caller may call it, and never names a role. A role is a set of permissions.**
How an API writes each operation's security requirement into the OpenAPI document it
generates is `BackendCode.md` section 19's.

**An operation is reached only by a caller its security requirement allows, or by any caller
where the operation is declared public. An operation that declares neither is reached by no
caller.** How an API declares an operation's security requirement, and checks it before the
request is read, is `BackendCode.md` section 6's.

## Records, fields and tenants

**A caller reaches a record only where a rule grants it that record. Holding a record's id
grants nothing.** Where the rule that grants a record is decided is `BackendCode.md` section
4's, which puts a business rule in the Domain.

**A caller reads only the fields a rule lets it read, and changes only the fields a rule lets
it change.**

**A caller never reads or changes anything belonging to a tenant, or to a company inside a
tenant, that it is not authorised for.**

## Where access is decided

**Every access decision is made by a back end service. What a frontend shows or hides decides
nothing.**

## Withdrawn access

**When a user loses a permission or a role, it stops working from that user's next request.**

## The database

**Each API connects to the database under a database role of its own, granted only what that
API needs, and never the right to change the schema.** How a table is granted to an API's
database role is `BackendCode.md` section 13's.

# 7. Tokens

## Self-contained tokens

**A self-contained token is a token that carries everything it states in itself, and that a
service trusts by checking its signature or its MAC, never by looking it up.** A JSON Web
Token (JWT) is an example of a self-contained token.

**A self-contained token is trusted only after its signature or its MAC is checked, and a
token that fails the check is refused.**

**A self-contained token is made and checked only with an algorithm that the cryptographic
inventory section 3 names lists for that kind of token. That list never holds `none`. Where
it holds both a symmetric and an asymmetric algorithm, a key meant for one is never used as
the other.**

**The key that checks a self-contained token comes only from a source configured in advance
for that token's issuer. A key source the token itself names is used only when it is one of
those sources.**

**A self-contained token is accepted only within the validity period it carries.**

**A token is accepted only for the use its type is meant for. An API accepts only an
authentication token, and never a refresh token or a sign-in assertion in its place.**

**A service accepts a token only where the token is meant for that service, whether the token
names its audience itself or its issuer says so when asked.**

**Where one key signs tokens for more than one audience, each token names the audiences it is
for. An audience added while the product runs is checked before any token names it.**

## Where a token is sent and kept

**A token is sent only to the parts of the product that need it. An authentication token goes
only to the APIs it is meant for, and a refresh token only to the identity service.**

**An authentication token in a browser is kept in memory only. A refresh token is kept only in
a cookie that script cannot read. Neither is kept after the tab closes unless the person asked
to stay signed in.** Where a frontend keeps a token is `FrontendCode.md` section
10's.

## How an API reads a token

**Where an authentication token says what its caller was granted, such as its subject or its
scopes, every access decision takes that into account.**

**A user is identified from a token only by a value its issuer never gives to anyone else,
read together with that issuer.**

## The identity service

**The identity service sends a browser back only to an address registered in advance for the
frontend or system that asked, matched character for character.**

**An authorization code is exchanged for tokens once. A second attempt is refused, and every
token issued from that code is refused from then on.**

**An authorization code lives at most 10 minutes.**

**The identity service lets each frontend and system caller use only the grants it needs. The
implicit grant and the password grant are never offered.**

**The identity service requires a proof key on every authorization code request, refuses the
plain method, and checks the verifier when the code is exchanged.**

**Where a system can register itself with the identity service without signing in, its
registered addresses and details are checked, and a person is warned and asked to consent
before that system is used.**

**Every request a system caller sends to the identity service, such as to obtain a token or to
withdraw one, carries its identifier and its secret.** The identity service checks the two
together, as section 4 states.

**The identity service grants each frontend and system caller only the scopes it needs.**

## Refresh tokens

**A refresh token held by a frontend is either bound to that frontend, or works once. When a
refresh token that was already used is presented again, every refresh token of that sign-in
is refused from then on.**

**A refresh token stops working when its session ends, and never later.**

## Answers to a sign-in

**The identity service and each frontend accept an authorization code or a sign-in assertion
only in answer to a sign-in that the same browser started. The proof key, the state value and
the nonce are random, made for that one sign-in, and bound to whoever started it and to that
browser.**

**Where a person can be signed in by more than one identity provider, the identity service
checks in every response which identity provider sent it.**

**The identity service accepts an OpenID Connect sign-in assertion only where it carries the
nonce the identity service sent for that sign-in.**

**The identity service refuses an identity provider's published metadata unless the issuer it
names matches exactly the issuer configured for that identity provider.**

**The identity service accepts an OpenID Connect sign-in assertion only where its audience is
exactly the identity service's own identifier at that identity provider.**

**Where an identity provider's server tells the identity service that a person signed out, the
identity service accepts only a logout token of the logout type, carrying the logout event and
no nonce.**

## Where the identity service is an OpenID Connect provider

**Where the identity service is an OpenID Connect provider for another system, it allows only
the response types `code`, `ciba`, `id_token` and `id_token code`, and never `token`.**

**Where the identity service is an OpenID Connect provider for another system, it ends a
sign-in at that system's request only after the person confirms it, or after checking the
`id_token_hint` the request carries.**

## Consent

**The identity service grants nothing on a person's behalf without that person's consent.
Where it cannot be sure which frontend or system is asking, it asks the person every time.**

**When a person is asked to consent, they are shown what is being granted, to which
application, and for how long.**

**A person can see, change and withdraw every consent they have given.**

# 8. Input and output

## Decoding and encoding

**A value received from outside is decoded once, where it arrives, before it is checked, and
is never decoded again.** That an operation never decodes a value the framework has already
decoded is `BackendCode.md` section 8's.

**A value received from outside never changes the structure of anything the product builds
with it, such as a database query, a command, a directory query, a URL, a header, a page, a
JSON or XML document, a regular expression, a template or a mail message.** **A value is
encoded or escaped only where it is written, by what writes it or by the interpreter that
reads it, and it is stored as it was received, never encoded.** That a frontend never writes
HTML from data is `FrontendCode.md` section 16's. That a handler never returns JSON it built as
text is `BackendCode.md` section 7's. That a database query is protected against
injection is `BackendCode.md` section 21's.

**A value placed into a URL is encoded for its place in that URL. An address taken whole from
data received from outside is used as a link or a redirect only when its scheme is `https` or
`http`.** That a frontend's call to the server encodes a value for its place in the URL is
`FrontendCode.md` section 8's, which governs how a frontend calls a server. That a frontend
never renders a link whose scheme is unsafe is `FrontendCode.md` section 16's.

**HTML received from outside, such as from a rich text editor, is sanitised by a well-known
HTML sanitisation library before it is stored, and only what the sanitiser keeps is ever
shown.**

**No code is built or run from data received from outside.** That a frontend never builds or
runs code this way is `FrontendCode.md` section 16's.

**An SVG received from outside is served or shown only after everything but drawing elements
and their attributes is removed, every script and every `foreignObject` included.**

**Markdown, CSS, XSL, BBCode or any similar content received from outside is sanitised before
it is shown, or is shown as plain text.**

**The product calls an address that depends on data received from outside only when its
scheme, host, port and path are on the list its feature's plan states for that call.**

**No template, such as the template of an email, is built from data received from outside. A
value from outside only ever fills a template.**

**A format string is never taken from data received from outside.** That a log message is
never built with string interpolation is `BackendCode.md` section 15's, whose message is a
template with every value named and passed as an argument.

**The product runs no code that reads or writes memory without the runtime checking its
bounds.**

**XML received from outside is read with document type definitions refused and external
entities never resolved.**

**Data received from outside is never turned into an object of a type the data itself names,
and never by a mechanism its own documentation calls insecure.**

## Validation and business logic

**Every field an operation receives has the rules that make it valid stated in its
contract.**

**Every value the product receives from outside is checked against the rules stated for it
before it is used, and a value that breaks them is refused. That includes every field of a
request, its route and its query, every header the product reads, every file, and everything a
service outside the product returns.** That every request has its own validator, and a request
that fails validation never reaches the handler, is `BackendCode.md` section 8's. That a
validator states each field's longest length and every number's range is `BackendCode.md`
section 8's. That a validator holds a rule across two fields of the same request is
`BackendCode.md` section 8's, and that a rule which depends on what is stored is in the Domain
is section 4's. That a provider's service checks what its provider returns before a handler
uses it is `BackendCode.md` section 9's. That the server is the only authority, and a
rule that exists only in the browser is a rule that does not exist, is `FrontendCode.md`
section 11's.

**A step of a business process is refused until every step that must come before it is
complete, whatever order the requests arrive in.**

**Every limit a feature's plan states is enforced, for each user and across the product, as the
plan states it.**

**A limited quantity, such as a customer's seats, is never used beyond its limit, including by
requests that arrive at the same moment.** That an operation reads and saves so that two
requests changing the same limited quantity at the same moment cannot both succeed beyond it is
`BackendCode.md` section 10's.

**Every API limits how many requests each caller may make in a period: each signed-in person,
each customer's system, and each network address calling an operation that needs no signed-in
caller. A request over its limit is refused before any work is done for it. Signing in is
limited only by this document's rules on failed sign-ins.** That an API reads its limits from
its settings, and the status and code a refused request carries, is `BackendCode.md` section
9's.

# 9. The API and files

## Content types and responses

**Every response with a body declares a Content-Type that matches the body, and a text or XML
type names its character set.** That the documentation page's Content-Type names its character
set is `BackendCode.md` section 19's.

**An API never redirects a request from HTTP to HTTPS. Only a frontend's address may.**

**A header that a part of the product in front of an API adds, such as the caller's network
address, is used only as that part set it, and never as the caller sent it.**

**Every part of the product that receives HTTP, anything in front of an API included, finds
where a message ends as its HTTP version requires: in HTTP/1.1 it ignores Content-Length when
Transfer-Encoding is present, and in HTTP/2 and HTTP/3 it refuses a Content-Length that does
not match the data.**

## WebSockets

**A WebSocket connection is made only over TLS.**

**A WebSocket connection is accepted only when its Origin is one of the product's own
frontends.**

**A WebSocket connection is signed in by the product's own session, or by a token that meets
every rule this document states for a session.**

**A token that signs in a WebSocket connection is obtained through a request already signed in
over HTTPS.**

## Files

**Every operation that accepts a file states in its contract the file types and extensions it
accepts, the largest size it accepts, and for an archive the largest unpacked size and the most
files.**

**Every operation that accepts a file has a largest size it accepts, and a larger file is
refused before it is stored.** That the response refusing a file too large carries a code, with
a row in the status table for it, is `BackendCode.md` section 7's.

**A file is accepted only when its extension is one its operation accepts and its content is of
the type that extension names. The type a caller declares for a file is never trusted.**

**An archive, a file format that is one included, such as a `.docx`, is checked against the
largest unpacked size and the most files stated for its operation before it is unpacked.**

**A file received from outside is never stored where a web server serves files directly, and is
never run.**

**The name and the path under which a file is stored are made by the product, and never contain
the name a caller sent.**

**A file the product serves names its download in a Content-Disposition header, with a name the
product has checked or made.**

**Every file received from outside is scanned for malware before it is stored, and again before
each time it is served. A file found to hold malware when it is received is refused, and
nothing of it is stored. A stored file found to hold malware when it is served is not served,
and is never served again. When malware is found in a file, the person who uploaded it and the
administrator of the customer they belong to are notified. A file that cannot be scanned is
neither stored nor served.**

## Documentation and monitoring

**An API that only the product's own frontends and services call publishes its OpenAPI document
and its documentation page in every environment except production. An API that a customer's
system calls publishes both in every environment, production included. A published OpenAPI
document and its documentation page need no signed-in caller.**

**An endpoint that monitors the product is an operation like any other, and reaches only the
callers its contract states.**

# 10. The browser

## What a browser shows

**A browser never shows a response from an API as a page, except the page over an API's
documentation. A file the product serves is sent as an attachment. A file received from
outside is never shown as a page at the address of a frontend.** That a frontend never opens
a file it fetched from an API as a page is `FrontendCode.md` section 16's.

## Cookies

**Every cookie the product writes is marked `Secure`. Its name begins with `__Host-`, or with
`__Secure-` where the cookie is meant to be sent to more than one host.**

**Every cookie the product writes states its `SameSite` attribute. The value is `Strict`,
unless the plan for the feature that writes the cookie states another value for it.**

**A cookie that script does not read is marked `HttpOnly`, and its value reaches the browser
only in a `Set-Cookie` header, never in a response body.**

## Headers and the Content-Security-Policy

**Every response the product sends carries a `Strict-Transport-Security` header with a
`max-age` of at least one year and `includeSubDomains`.**

**An API lets a page at another origin read its responses only when that origin is on the
API's list. The list holds only the addresses of the product's own frontends that call that
API. No response lets every origin read it.** Where an API's list of allowed origins is read
from, and that nothing else is allowed, is `BackendCode.md` section 9's.

**Every page the product serves carries a Content-Security-Policy under which the page loads
nothing, and connects to nothing, except its own address and the addresses of the APIs it
calls. It sets `object-src` and `base-uri` to `'none'`, runs script only from its own address,
and runs no inline script and no code built from text. It may also show an image that its own
script fetched and made into an object URL.**

**The page over an API's documentation may also run its one inline start script, allowed by
that script's hash.**

**Every response that is not a page carries a Content-Security-Policy that allows nothing to
load and no page to frame it.**

**Every response the product sends carries `X-Content-Type-Options: nosniff`.**

**Every page the product serves sets its referrer policy to `no-referrer`.**

**Every response the product sends carries a Content-Security-Policy whose `frame-ancestors`
is `'none'`.** Which headers an API writes on every response it sends, the documentation
page's included, is `BackendCode.md` section 7's.

## Requests, messages and methods

**An operation that a cookie signs in refuses a request that did not come from one of the
product's own frontends.**

**A message received through `postMessage` is used only when it comes from an origin of the
product's own and its content is valid. Any other is discarded.**

**An operation that changes anything, or that produces an export of data, is never reached
with `GET`, `HEAD` or `OPTIONS`.**

## Where a frontend and an API are served, and where a browser is sent

**Each frontend and each API is served at a host name that no other frontend or API shares.**

**The product sends a browser automatically to a host the product does not control only when
that host is on the list its feature's plan states.**

# 11. Transport, configuration and infrastructure

## TLS and encrypted connections

**In a deployed environment, every connection to or from a part of the product uses TLS 1.3,
and no earlier version is enabled.**

**Where more than one cipher suite is offered, the strongest is preferred.**

**A client certificate is checked as trusted before the identity it carries signs a caller in
or decides what the caller may do.**

**An address reached from outside the product is served only over TLS. A frontend's address
answers plain HTTP only by redirecting to HTTPS, and an API's address never answers plain
HTTP.**

**An address reached from outside the product presents a certificate issued by a publicly
trusted certificate authority.**

**Every connection to or from a part of the product is encrypted, and none falls back to an
unencrypted connection. That includes a connection to the database, to the vault, to a service
outside the product, to logging and monitoring, from anything in front of an API to that API,
and every way a person administers a server.** The connection string an API and the migrator
use to reach the database is `BackendCode.md` section 14's.

**Before the product sends anything over a connection, it checks the certificate it received:
that an authority it trusts issued it, that it names the host connected to, and that it is in
date. Every signature the product makes, and every signature made during a TLS handshake to
prove a key, uses only what Appendix C of ASVS 5.0.0 approves. The one exception is a certificate's own
signature, made once by the authority that issued it: a certificate is never refused only
because that signature is not one Appendix C of ASVS 5.0.0 approves.** That a provider's service never turns
off or replaces this check is `BackendCode.md` section 9's.

**A connection between parts of the product presents a certificate issued by a publicly
trusted certificate authority, or by an internal certificate authority that only the parts
making that connection trust.**

## The infrastructure inventory

**A list of every part of the product, every service outside it that the product connects to,
and every connection between them, is kept in `Docs/InfrastructureInventory.md`. It states for
each part what it is, which environments hold it, the highest protection level of the data it
holds or carries, and whether the internet can reach it. It states for each connection which
part connects to which, over which port and protocol, the highest protection level of the data
it carries, and how the connecting part proves who it is. Where the address a connection
reaches depends on data received from outside, it names the feature whose plan states
what that address may be. It is changed in the same increment as what it lists.**

## How one part of the product proves itself to another

**In a deployed environment, a part of the product proves who it is to another part with a
certificate or with a token that expires, and never by sending that part a password or a key
that stays the same. That includes the database and the vault. A service account's password,
or an API's secret key, is presented only to the identity service, which returns an
authentication token. A call to another part carries that token.** That the database
connection string carries no password in a deployed environment is `BackendCode.md`
section 14's.

**Every part of the product runs under an operating system account that is not an
administrator of the machine or the container it runs in.**

**No part of the product signs in to another part, or to a service outside it, with a
credential its supplier set by default.**

## What each part may connect to, and network segmentation

**Each part of the product can connect only to what that list gives it, and any other
connection it attempts is refused.**

**Each deployed environment runs on a network of its own, and no part of one environment can
reach a part of another.**

**In production, the internet can reach only the address of a frontend and the address of an
API that a frontend or a customer's system calls. It cannot reach the database, the vault, or
any other part of the product.**

**An environment other than production cannot be reached from the internet. Only the people
and systems named for that environment can reach it, over a connection that proves who they
are.**

**Every address the internet can reach sits behind a boundary that lets through only the ports
and protocols that address serves, and refuses everything else.**

## Deployed environments

**No deployed part of the product holds source control metadata, such as a `.git` folder.**

**No part of the product runs in production with a debug or development mode turned on, and no
frontend in production serves a source map.**

**No address the product serves lists the files in a folder.**

**No address the product serves answers the `TRACE` method.**

## Administrators

**In a deployed environment, only the people named as that environment's administrators can
sign in to its servers, its database and its network elements. Each signs in as themselves,
with a second factor, and every sign-in and every change is recorded.**

## WebRTC

**A TURN service the product runs relays only to addresses not reserved for a special purpose,
such as a private network, loopback or broadcast address, in IPv4 and IPv6 alike.**

**A media server the product runs checks the authentication of every SRTP packet, and drops a
packet that fails.**

**A media server the product runs keeps processing media when it receives a malformed SRTP
packet.**

**A signaling server the product runs limits how many messages each caller may send in a
period, and keeps processing legitimate messages during a flood.**

**A signaling server the product runs keeps processing legitimate messages when it receives a
malformed one.**

# 12. Logging, errors and incidents

## What a log holds

**Every log line names when it was written, in UTC, and which part of the product wrote it. A
line written while a request is served also names that request, the caller, being the key of
the signed-in user or the identifier of the system caller, and the network address the request
came from.** The properties every log line carries are `BackendCode.md` section 15's.

**No value at either protection level, and no secret, is written to a log in any form, hashed,
masked or encoded included. A log may hold a record's key, and the network address a request
came from.** What a handler passes to a log call, what the framework writes by itself, and what
the database libraries write are `BackendCode.md` section 15's.

**Every part of the product keeps its clock synchronised with one common time source, and
writes every time in its logs in UTC.** The timestamp every log line carries is
`BackendCode.md` section 15's.

**Every part of the product writes its logs in a format the log store reads field by field.
Every line that an API, or anything in front of an API, writes while serving a request carries
that request's trace id.** The format every log line uses, and the trace id it carries, is
`BackendCode.md` section 15's.

**A part of the product writes or sends its logs only to the places
`Docs/InfrastructureInventory.md` lists for it.**

**A value written into a log can never begin a new line or change another field of a line.**
How a log call writes a value so it cannot begin a new line is `BackendCode.md`
section 15's.

**Every part of the product sends its logs, as it writes them, to a log store that runs apart
from every part whose logs it holds. No part of the product can change or delete a log once it
has sent it.**

## Security events and the access record

**The product records each of these as a security event: a sign-in attempt and its outcome,
with the factors used; a user being locked and unlocked; a session ending, with the reason; a
password being set or reset; a second factor being added, replaced or removed; an invitation
being sent, used or cancelled; a system caller failing to authenticate; an authentication token
being refused; a request refused with 401 or 403; a change to a user's permissions or roles; a
change to a customer's security setting; a request refused by validation, with the code of the
refusal; a request refused for being over its limit; a file refused for holding malware; an
error the product did not expect; and a security control failing, such as a certificate check,
the check of a file for malware, reading the vault, or sending logs. A person reading or
changing a deployed environment's vault, and a person signing in to one of its servers, its
database or its network elements, is recorded as a security event. Each security event is
written by the part of the product where it happens.** The lines an API writes for a security
event, and their levels, are `BackendCode.md` section 15's.

**Every operation that returns or changes data at either protection level records who called
it, when, which operation it was, and the key of every person record whose data it returned or
changed. For any caller and any period, the product can produce the person records that caller
was given or changed, and for any person, the callers that were given or changed their records.
For the people a compromise reached, the product can produce each person's last known email
address, and their physical or postal address, where it holds them.**

## Who reads the logs, and how long they are kept

**In a deployed environment, the logs are read only by the people named as that environment's
administrators. A customer can be given the changes and the reads recorded for its own tenant,
and never for another tenant. Which users of that customer may see them is the permission its
feature states. No one can change a log line or an access record, or remove one before its
period ends, and the environment's administrators are no exception. A read of the access record
is itself recorded.**

**A security event and an access record are kept for 12 months, and are removed when that
period ends. Every other log line is kept for 30 days, and is removed when that period ends.
Both periods are the same in every deployed environment.**

## Alerts

**Each of these raises an alert that reaches a person who can act on it: failed sign-in
attempts spread across many users, from one network address or from many; a system caller
failing to authenticate repeatedly; a person reading or changing a deployed environment's
vault; a person signing in to a deployed environment's server, database or network element; one
caller being given far more person records in a period than the operation it called normally
returns; what a boundary protection system reports as an attempt to get past it; a security
control failing, such as a certificate check, the check of a file for malware, reading the
vault, or sending logs; a part of the product no longer sending its logs, and the alerting
itself no longer working; and a rise above what is normal for that part in errors the product
did not expect or in requests refused with 401 or 403. Where one of those conditions needs a
number, that number is set for each deployed environment and is not the same for every part.**

## Errors

**What the product shows or sends for an error it did not expect is a fixed message, and never
an exception's message, a stack trace, a database query or a fragment of one, a secret or a
token. That includes a page a frontend shows and a response sent by anything in front of an
API.** What an API's error response may hold is `BackendCode.md` section 7's. What a frontend
shows for an error no code expected is `FrontendCode.md` section 9's.

**When a service the product depends on cannot be reached, the product refuses what needs that
service and keeps serving everything else.** How a provider's service keeps operating when it
cannot be reached is `BackendCode.md` section 9's.

**An error never lets a request past a check it did not pass. A change a request makes to the
database is saved whole or not at all.** What a provider's service does when it cannot reach
its provider or cannot use its answer is `BackendCode.md` section 9's.

# 13. Dependencies and secure coding

## Vulnerable components and their periods

**A build of any part of the product fails when a package it uses, directly or through
another package, carries a known vulnerability at any severity. A build fails when it cannot
get vulnerability data for a package source it uses. A vulnerability that cannot be fixed is
accepted only by an entry in `Docs/AcceptedVulnerabilities.md`, naming the finding, the
package, why it cannot be fixed, the date it was accepted and the date that acceptance ends.
A build fails on a finding it does not hold, on an entry whose date has passed, and on an
entry whose finding the dependency audit no longer reports.**

**`Docs/AcceptedVulnerabilities.md` is changed in the same increment as what it lists.**

**A critical or high finding is accepted for at most 30 days, and a moderate, low or info
finding for at most 12 months.**

**Every third-party library the product uses is brought to a current version at least once
every 12 months, planned as work of its own.**

## The software list and its sources

**Each time a part of the product is built, the build produces a list of every third-party
component in it, directly and through another component, with each component's version. The
list is kept with the built thing and is named by the value that built thing carries.**

**Every package a build uses comes from a source the repository names, and a build resolves a
package from no other source. A package source the product uses publishes vulnerability data
for what it serves.**

## Slow or costly work

**An operation that cannot answer within the time its caller waits does not hold the request
open while it works. How many of a slow or costly operation may run at once is limited, for
each caller and for the product as a whole.**

## Minimal deployments

**A deployed part of the product holds only what it needs to serve, and no test, sample or
development code.**

## Limiting what an operation returns, writes and calls

That an operation projects to its response type inside the query, and never loads a whole
entity and maps it afterwards, is `BackendCode.md` section 10's.

**A call the product makes out of the process does not follow a redirect, unless the plan
of the feature that makes the call states that it does and states the addresses the
redirect may reach.** That a provider's service is wired to follow no redirect is
`BackendCode.md` section 9's.

That an action writes only through the fields its own request type declares is
`BackendCode.md` section 18's.

**An API reads the caller's network address from the part of the product in front of it, and
never from a header the caller can set. That address is the one written to the logs and the
one every limit on a caller is counted against.** That the startup wiring reads the caller's
address from what is in front of the API is `BackendCode.md` section 9's.

## Repeated values

**A request that carries the same value more than once is refused, unless the operation's
contract declares that value as one that takes many.** That the wiring refuses a request
carrying the same value more than once is `BackendCode.md` section 7's.

# 14. Change, build and deployment

## The checks a change passes

**Before a change can reach a deployed environment it passes every check below, each run as
a step of its own, and a change that fails any of them is never deployed. The checks are:**

- the build of every part of the product;
- the tests;
- the dependency audit;
- secret scanning;
- and the install that resolves no version of any package for itself.

**No check is skipped, turned off, or its result set aside for one change.**

**A check whose tool only reports what it found is read by the step that runs it, and that
step fails the change.**

**Secret scanning reads both what the repository holds now and every earlier version of it.**

**A secret found in the repository is replaced at once, and removing it from the repository
is not what replaces it.**

**A finding that is not a secret is accepted only by an entry in the repository that names
that one finding.**

## How a deployment reaches an environment

That a change is approved, and reaches a deployed environment only through a pull request a
person merges, is `Branching.md` section 1's.

**A deployed environment runs only what the product's own build produced from a change that
passed every check. No person puts code or a package onto a deployed environment.**

**Each part of the product is deployed as one built thing that is not changed after it is
built, and that is identified by a value derived from its contents. Only a part's settings
and its secrets differ between environments.**

**A change that has to happen urgently passes the same checks as any other, and reaches a
deployed environment by the same path.**

**Every deployed environment can be built again from what the repository holds, with that
environment's own settings and its own secrets.**

## The vault credential

**The credential a part of the product uses to reach the vault is kept on the deployed
environment it belongs to, in a form that cannot be read back, and in no other place. That
credential is replaced at least once every 12 months, and at once when it may have been
exposed or when a person who could reach it stops being one of that environment's named
administrators.**

## Preventing unauthorised change

**A deployed environment takes a deployment only from the product's own build, and no other
way of deploying to it is enabled.**

**What runs in production is deployed as a deliberate act of its own, and never because a
change was merged.**

**Every input to a build is fixed, so that two builds of the same change produce the same
components.**

**Each built thing's list is compared with the list of the one before it, and a component
that neither the repository nor that comparison accounts for is reported.**

## Scanning the running product

**Every deployed environment is scanned for anything it serves that it should not, at least
once a day and after each deployment. The scan is pointed at the root of each part and at
each of its operations.**

**A scan of production only reads what the product serves, and never sends a request meant
to break it.**

**A finding raises an alert, and the finding is read from what the scan reports and not from
whether the scan succeeded.**

**A scan reaches nothing outside the product: no service on the public internet, and no port
the list of infrastructure does not give it.**

# 15. Enforcers

Every rule in this document has an enforcer, and this section names it. Where that thing is
nothing, the last list says so, because a column that claims coverage it does not have is
worse than no column.

The three parts below are one question asked three ways: is the rule caught without anyone
reading the increment, only by reading it, or not at all.

## What is caught without reading the increment

| Rule | Enforcer |
|---|---|
| Every value is a floor, and a customer is never given a weaker setting | Planning, which states in a feature's plan what a customer may change, and `feature-review` |
| No rule is sold as an upgrade | Planning, which plans a paid feature, and `feature-review` |
| No payment card number reaches the product | Planning, which plans every field a feature stores, and `feature-review` and the person who reviews the increment |
| Data about a person is at one of two protection levels | Planning, which states the level of every field in the plan, and `feature-review` |
| Data at either protection level is never put in a URL | Planning, which writes every route and query parameter into the contract, and `feature-review` and the person who reviews the increment |
| A secret is never put in a URL | Planning, which writes every route and query parameter into the contract, and `feature-review` and the person who reviews the increment |
| Data at either protection level is stored encrypted in each column its plan names | Planning, which names those columns in the plan, and `feature-review` and the person who reviews the increment |
| Every uploaded document is encrypted before it is stored | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| The database is backed up daily | A decision in a plan, which states how it is met, and `feature-review` |
| Every backup is encrypted | A decision in a plan, which states how it is met, and `feature-review` |
| Every record of personal information has a stated period | Planning, which states each category's period in the plan, and `feature-review` |
| A record used to make a decision about a person is kept long enough to be asked for | Planning, which states the period in the plan, and `feature-review` |
| A record that may no longer be kept is de-identified within one day, and no row at either protection level is deleted | Planning, which names a column's de-identified value where the default does not fit, and `feature-review` and the person who reviews the increment |
| A de-identified value cannot be turned back, no backup is kept longer than 30 days, and a restore carries out every de-identification again | A decision in a plan, which states how backups and restores meet it, planning, which names the de-identified values, and `feature-review` and the person who reviews the increment |
| Personal information can be corrected or de-identified | Planning, which states in the plan how each category is corrected and de-identified, and `feature-review` |
| No secret is in the repository or in anything a build produces | Secret scanning, one of the checks section 14 states, which reads every version of the repository; and `feature-review`, for what a build produces and for a secret the scan does not recognise |
| A secret the product itself uses is kept in the vault | A decision in a plan, which states how it is met, and `feature-review` |
| Each part of the product reads only its own secrets, under its own identity | A decision in a plan, which states how it is met, and `feature-review` |
| Only a deployed environment's named administrators reach its vault, and every read and change is recorded | A decision in a plan, which states how it is met |
| A shared secret key is held by no more than two parties, and a private key by one | A decision in a plan, which states how it is met, and `feature-review` |
| How a key is made, kept, replaced and destroyed | A decision in a plan, which states how it is met, and `feature-review` |
| Every secret kept in the vault is replaced at least once every 12 months, except a customer's secret | A decision in a plan, which states how it is met, and `feature-review` |
| Every cryptographic setting can be changed, and every key and secret replaced without losing data | A decision in a plan, which states how it is met, and `feature-review` |
| No caller signs in by a way this document does not list | A decision in a plan, which states how each way is met, and `feature-review` and the person who reviews the increment |
| A person who has lost a factor is given a new one only after their identity is confirmed as strongly as when the user was created | Planning, which states in the plan how a new user's identity is confirmed, and `feature-review` |
| A system caller proves which caller it is, and is never taken on trust for being inside our own network | A decision in a plan, which states how it is met, and `feature-review` |
| A system caller's credential is random and long, kept only in the vault, and never typed by a person | A decision in a plan, which states how it is met, and `feature-review` |
| A key that signs an authentication token is held only by the product | A decision in a plan, which states how it is met, and `feature-review` |
| A user reaches a customer's records only where someone that customer authorised has granted it | Planning, which states in the plan who authorises a new user's access, and `feature-review` |
| Every session token is verified by a back end service | A decision in a plan, which states how it is met, and `feature-review` |
| An operation's security requirement names permissions, and never a role | Planning, which writes every operation's security requirement into its contract, and `feature-review` |
| An operation is reached only by a caller its security requirement allows, and one that declares neither is reached by no caller | `Testing.md` section 8's 401 and 403 sweeps, which prove it for a signed-out caller and for each fixed role account, and `feature-review` |
| A caller reaches a record only where a rule grants it that record | Planning, which states in the plan who may see each record, `Testing.md` section 8's ownership tests and coverage test, and `feature-review` and the person who reviews the increment |
| A caller reads and changes only the fields a rule lets it | Planning, which states in the plan who may read and change each field whose access differs from its record's, and `feature-review` and the person who reviews the increment |
| A caller never reaches a tenant or a company it is not authorised for | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| A lost permission or role stops working from the user's next request | A decision in a plan, which states how it is met, and `feature-review` |
| Each API connects under a database role of its own, granted only what it needs, and never the right to change the schema | A decision in a plan, which states how it is met, and `feature-review` |
| A token is sent only to the parts of the product that need it | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| An authentication token in a browser is kept in memory only, and a refresh token only in a cookie that script cannot read | A decision in a plan, which states how it is met, and `feature-review` |
| Every field an operation receives has the rules that make it valid stated in its contract | Planning, which states those rules in the contract's document, and `feature-review` and the person who reviews the increment |
| A step of a business process is refused until every step that must come before it is complete | Planning, which states the order of a feature's steps in the plan, and `feature-review` and the person who reviews the increment |
| Every limit a feature's plan states is enforced, for each user and across the product | Planning, which states each limit in the plan, and `feature-review` and the person who reviews the increment |
| The product calls an address that depends on data received from outside only when it is on the list its plan states | Planning, which states the list in the plan, and `feature-review` and the person who reviews the increment |
| Every API limits how many requests each caller may make in a period | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| Every part of the product that receives HTTP finds where a message ends as its HTTP version requires | A decision in a plan, which states how it is met |
| Every operation that accepts a file states in its contract the file types and extensions, the largest size, and for an archive the largest unpacked size and the most files | Planning, which states those values in the contract's document, and `feature-review` and the person who reviews the increment |
| Every file received from outside is scanned for malware before it is stored and before each time it is served | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| Every cookie the product writes states its `SameSite` attribute, at the value the plan for the feature that writes it states | Planning, which states each cookie's `SameSite` value in the plan, and `feature-review` and the person who reviews the increment |
| An operation that changes anything, or that produces an export of data, is never reached with `GET`, `HEAD` or `OPTIONS` | Planning, which declares each such operation's method in its contract, and `feature-review` and the person who reviews the increment |
| The product sends a browser automatically to a host the product does not control only when that host is on the feature's list | Planning, which states the list of hosts in the plan, and `feature-review` and the person who reviews the increment |
| Each frontend and each API is served at a host name that no other frontend or API shares | A decision in a plan, which states how it is met, and `feature-review` |
| In a deployed environment, every connection to or from a part of the product uses TLS 1.3, and no earlier version is enabled | A decision in a plan, which states how it is met, and `feature-review` |
| Where more than one cipher suite is offered, the strongest is preferred | A decision in a plan, which states how it is met, and `feature-review` |
| An address reached from outside the product presents a certificate issued by a publicly trusted certificate authority | A decision in a plan, which states how it is met, and `feature-review` |
| Every connection to or from a part of the product is encrypted, and none falls back to an unencrypted connection | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| A connection between parts of the product presents a certificate issued by a publicly trusted certificate authority, or by an internal authority the connecting parts trust | A decision in a plan, which states how it is met, and `feature-review` |
| A part of the product proves who it is to another part with a certificate or with a token that expires, and never with a password or a key that stays the same | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| Every part of the product runs under an operating system account that is not an administrator of the machine or the container it runs in | A decision in a plan, which states how it is met, and `feature-review` |
| No part of the product signs in to another part, or to a service outside it, with a credential its supplier set by default | A decision in a plan, which states how it is met, and `feature-review` |
| Each part of the product can connect only to what its list gives it, and any other connection it attempts is refused | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| Each deployed environment runs on a network of its own, and no part of one environment can reach a part of another | A decision in a plan, which states how it is met, and `feature-review` |
| In production, the internet can reach only the address of a frontend and the address of an API a frontend or a customer's system calls | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| An environment other than production cannot be reached from the internet, except by the people and systems named for it | A decision in a plan, which states how it is met, and `feature-review` |
| Every address the internet can reach sits behind a boundary that lets through only the ports and protocols that address serves | A decision in a plan, which states how it is met, and `feature-review` |
| No deployed part of the product holds source control metadata | A decision in a plan, which states how it is met, and `feature-review` |
| No part of the product runs in production with a debug or development mode turned on, and no frontend in production serves a source map | A decision in a plan, which states how it is met, and `feature-review` |
| No address the product serves lists the files in a folder | A decision in a plan, which states how it is met, and `feature-review` |
| No address the product serves answers the `TRACE` method | A decision in a plan, which states how it is met, and `feature-review` |
| Only a deployed environment's named administrators can sign in to its servers, its database and its network elements, each as themselves with a second factor, with every sign-in and every change recorded | A decision in a plan, which states how it is met, and `feature-review` |
| A list of every part of the product, every service it connects to and every connection between them is kept in `Docs/InfrastructureInventory.md`, changed in the same increment as what it lists | Planning, which states each feature's change to it in the plan, and `feature-review` and the person who reviews the increment |
| Every log line names when it was written, which part of the product wrote it, and, on a request, the caller and the network address it came from | A decision in a plan, which states how it is met, and `feature-review` |
| Every part of the product keeps its clock synchronised, and writes every time in its logs in UTC | A decision in a plan, which states how it is met, and `feature-review` |
| A part of the product writes or sends its logs only to the places `Docs/InfrastructureInventory.md` lists for it | A decision in a plan, which states how it is met, and `feature-review` |
| Every log is written in a format the log store reads field by field, and every line inside a request carries that request's trace id | A decision in a plan, which states how it is met, and `feature-review` |
| The logs and the access record are read only by an environment's administrators, or by a customer for its own tenant, and neither can be changed or removed before its period ends | A decision in a plan, which states how it is met, and `feature-review` |
| A security event and an access record are kept for 12 months, and every other log line for 30 days | A decision in a plan, which states how it is met, and `feature-review` |
| Every part of the product sends its logs to a log store that runs apart from it, and no log can be changed or deleted once it has been sent | A decision in a plan, which states how it is met, and `feature-review` |
| Every operation that returns or changes data at either protection level records who called it, when, what it was, and whose records it touched | A decision in a plan, which states how it is met, `feature-review` and the person who reviews the increment |
| Each of the stated conditions raises an alert that reaches a person who can act on it | A decision in a plan, which states how it is met, and `feature-review` |
| What the product shows or sends for an error it did not expect is a fixed message, never an exception, a stack trace, a database fragment, a secret or a token | A decision in a plan, which states how it is met, `feature-review` and the person who reviews the increment |
| When a service the product depends on cannot be reached, the product refuses what needs it and keeps serving the rest | A decision in a plan, which states how it is met, `feature-review` and the person who reviews the increment |
| An error never lets a request past a check it did not pass, and a change a request makes to the database is saved whole or not at all | A decision in a plan, which states how it is met, `feature-review` and the person who reviews the increment |
| A build of any part of the product fails when a package it uses carries a known vulnerability, or when it cannot get vulnerability data for a package source | The dependency audit, one of the checks section 14 states, and `feature-review` |
| A vulnerability that cannot be fixed is accepted only by an entry in `Docs/AcceptedVulnerabilities.md` naming the finding, the package, why it cannot be fixed, the date it was accepted and the date that acceptance ends | The dependency audit, one of the checks section 14 states, and `feature-review` |
| A build fails on a finding `Docs/AcceptedVulnerabilities.md` does not hold, on an entry whose date has passed, and on an entry whose finding the dependency audit no longer reports | The dependency audit, one of the checks section 14 states |
| A critical or high finding is accepted for at most 30 days, and a moderate, low or info finding for at most 12 months | The dependency audit, one of the checks section 14 states, and `feature-review` |
| Each time a part of the product is built, the build produces a list of every third-party component in it, with its version | A decision in a plan, which states how the build produces the list, and `feature-review` |
| Every package a build uses comes from a source the repository names, and that source publishes vulnerability data for what it serves | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| An operation that cannot answer within the time its caller waits does not hold the request open, and how many of a slow or costly operation may run at once is limited | Planning, which states in the plan how a slow or costly operation is kept from exhausting the product, and `feature-review` and the person who reviews the increment |
| A deployed part of the product holds only what it needs to serve, and no test, sample or development code | A decision in a plan, which states how it is met, and `feature-review` |
| A call the product makes out of the process does not follow a redirect, unless the plan for the feature that makes the call states that it does | Planning, which states in the plan whether the call follows a redirect and which addresses it may reach, and `feature-review` and the person who reviews the increment |
| An API reads the caller's network address from the part of the product in front of it, and never from a header the caller can set | A decision in a plan, which states how it is met, and `feature-review` and the person who reviews the increment |
| A request that carries the same value more than once is refused, unless the operation's contract declares that value as one that takes many | Planning, which declares in the contract which values an operation takes as many, and `feature-review` and the person who reviews the increment |
| Every check a change must pass runs as a step of its own, and a change that fails any of them is never deployed | A decision in a plan, which states how it is met, and `feature-review` |
| A check whose tool only reports what it found is read by the step that runs it, and that step fails the change | A decision in a plan, which states how it is met, and `feature-review` |
| Secret scanning reads both what the repository holds now and every earlier version of it | A decision in a plan, which states how it is met, and `feature-review` |
| A finding that is not a secret is accepted only by an entry in the repository that names that one finding | A decision in a plan, which states how it is met, and `feature-review` |
| A deployed environment runs only what the product's own build produced from a change that passed every check | A decision in a plan, which states how it is met, and `feature-review` |
| Each part of the product is deployed as one built thing that is not changed after it is built, identified by a value derived from its contents | A decision in a plan, which states how it is met, and `feature-review` |
| A change that has to happen urgently passes the same checks as any other, and reaches a deployed environment by the same path | A decision in a plan, which states how it is met, and `feature-review` |
| Every deployed environment can be built again from what the repository holds | A decision in a plan, which states how it is met, and `feature-review` |
| A deployed environment takes a deployment only from the product's own build, and no other way of deploying to it is enabled | A decision in a plan, which states how it is met, and `feature-review` |
| What runs in production is deployed as a deliberate act of its own, and never because a change was merged | A decision in a plan, which states how it is met, and `feature-review` |
| Every input to a build is fixed, so that two builds of the same change produce the same components | A decision in a plan, which states how it is met, and `feature-review` |
| Each built thing's list is compared with the list of the one before it, and an unaccounted component is reported | A decision in a plan, which states how it is met, and `feature-review` |
| Every deployed environment is scanned for anything it serves that it should not, at least once a day and after each deployment | A decision in a plan, which states how it is met, and `feature-review` |
| A scan of production only reads what the product serves, and never sends a request meant to break it | A decision in a plan, which states how it is met, and `feature-review` |
| A finding raises an alert, read from what the scan reports and not from whether the scan succeeded | A decision in a plan, which states how it is met, and `feature-review` |
| A scan reaches nothing outside the product | A decision in a plan, which states how it is met, and `feature-review` |

## What is caught only by reading the increment

**Each rule below is caught by `feature-review`, and by the person who reviews the increment
where the row names them, and by nothing else.** Nothing on the build sees any of them, and
no test proves any of them. A rule reaching this part is not a weaker rule, and it is not
optional because the build is quiet about it.

| Rule | Enforcer |
|---|---|
| Data at either protection level is never kept by a cache | `feature-review` and the person who reviews the increment |
| Data at either protection level is never sent to a service that tracks people or measures use | `feature-review` and the person who reviews the increment |
| A frontend's published settings file holds no data at either protection level | `feature-review` and the person who reviews the increment |
| A response that carries data at either protection level tells the browser not to keep it | `feature-review` |
| Data at either protection level is never kept in the browser's storage or in a cookie | `feature-review` |
| When a session ends, a frontend removes everything it received while signed in | `feature-review` |
| A record kept for historical, statistical or research purposes is used for nothing else | `feature-review` |
| A record of personal information can be restricted | `feature-review` |
| A note that a correction was not made is shown with the information | `feature-review` |
| The product can produce every piece of personal information it holds about a person | `feature-review` |
| The cryptographic inventory is changed in the same increment as what it lists | `feature-review` and the person who reviews the increment |
| Every cryptographic mechanism gives at least 128 bits of security | `feature-review` and the person who reviews the increment |
| Cryptography uses only a mechanism Appendix C of ASVS 5.0.0 marks as approved | `feature-review` and the person who reviews the increment |
| Encrypted data is protected against being changed | `feature-review` and the person who reviews the increment |
| A password is stored only as a hash made by a password hashing function | `feature-review` and the person who reviews the increment |
| A hash used in a signature, or to prove data has not changed, has enough bits of output | `feature-review` and the person who reviews the increment |
| A key made from a password is made only by a password-based key derivation function | `feature-review` and the person who reviews the increment |
| A key is never generated in a way known to make it weak | `feature-review` |
| A random value that must not be guessed comes from a cryptographically secure generator, and is never a UUID | `feature-review` and the person who reviews the increment |
| Signing in as a user needs two or more factors | `feature-review` and the person who reviews the increment |
| A new user sets up a second factor before it reaches anything | `feature-review` and the person who reviews the increment |
| A lookup secret, an out-of-band code and a time-based one-time password each work once | `feature-review` and the person who reviews the increment |
| A lookup secret below 112 bits of entropy is stored only as a password hash with a salt | `feature-review` |
| A lookup secret, an out-of-band code and a one-time password seed come from a cryptographically secure generator | `feature-review` |
| A lookup secret and an out-of-band code carry at least 20 bits of entropy | `feature-review` |
| An out-of-band code lives at most 10 minutes, and a time-based one-time password at most 30 seconds | `feature-review` and the person who reviews the increment |
| A one-time password by text message or phone call is offered only for a confirmed number, beside a stronger method, with its risks explained | `feature-review` |
| An out-of-band request, code or token works only for the sign-in it was made for | `feature-review` and the person who reviews the increment |
| A password is refused when it contains a word of a kind this document names | `feature-review` |
| A password is at least 12 characters long | `feature-review` and the person who reviews the increment |
| The longest password accepted is at least 64 characters | `feature-review` and the person who reviews the increment |
| A password may hold any characters, and no rule requires a kind of character | `feature-review` and the person who reviews the increment |
| A password is checked exactly as entered | `feature-review` and the person who reviews the increment |
| A password among the 3,000 most common is refused | `feature-review` |
| A password known from breaches is refused | `feature-review` |
| No password expires on a schedule | `feature-review` and the person who reviews the increment |
| A person can change the password of any user they hold | `feature-review` |
| Changing a password requires the current password | `feature-review` and the person who reviews the increment |
| No password hint and no secret question exists | `feature-review` and the person who reviews the increment |
| Five failed attempts within 15 minutes lock the user for 15 minutes, counting the password and the second factor as one attempt | `feature-review` and the person who reviews the increment |
| Failed attempts across many users are detected | `feature-review` |
| A system caller is never locked by failed attempts | `feature-review` and the person who reviews the increment |
| A system caller's identifier and secret are checked together, and a failure never shows which was wrong | `feature-review` and the person who reviews the increment |
| A customer may set a lock that lasts until it is released, and a person can always release their own | `feature-review` |
| A failed sign-in never shows which part was wrong, and a sign-in name with no user behaves like one with a user | `feature-review` and the person who reviews the increment |
| An invitation or activation code is random, works once and expires after 72 hours, and the product never creates a password | `feature-review` and the person who reviews the increment |
| An invitation or activation code is never sent to a server in a URL | `feature-review` and the person who reviews the increment |
| A code is used up when the person sets their password, never when the link is opened | `feature-review` and the person who reviews the increment |
| A code is made by a cryptographically secure generator, holds at least 128 bits, and is stored only as a hash | `feature-review` and the person who reviews the increment |
| Resetting a forgotten password never gets past a second factor | `feature-review` and the person who reviews the increment |
| Where a customer's identity provider signs a user in, two factors are proved or asked for | `feature-review` and the person who reviews the increment |
| A person signed in by one identity provider is never taken for a person signed in by another | `feature-review` |
| The signature on every sign-in assertion is checked | `feature-review` and the person who reviews the increment |
| A SAML assertion is accepted once | `feature-review` |
| A stated strength or recentness of sign-in is checked against what the identity provider returned | `feature-review` and the person who reviews the increment |
| A fixed secret identifies a system caller only, and never carries a person's session | `feature-review` and the person who reviews the increment |
| A system caller's authentication token is refused once its secret is withdrawn | `feature-review` |
| The product holds no account that exists by default, and every account belongs to a person or to a part of the product | `feature-review` and the person who reviews the increment |
| When a user is disabled or deleted, its sign-in name and password stop working | `feature-review` |
| A session is for one user and is never widened to a second user | `feature-review` and the person who reviews the increment |
| A user holds at most 5 sessions at once | `feature-review` |
| No session is created unless the person acts or agrees | `feature-review` |
| A session ends after 30 minutes without activity | `feature-review` |
| A session ends one day after it began | `feature-review` |
| Every session follows this document's lifetimes whichever system signed the user in, and signing in again requires the identity provider to sign the person in again | `feature-review` and the person who reviews the increment |
| Sessions between the product and an identity provider end as this document states | `feature-review` |
| A person's session is carried by a token issued at sign-in, never by a fixed key or secret | `feature-review` and the person who reviews the increment |
| A reference token is unique, made by a cryptographically secure generator, and carries at least 128 bits | `feature-review` |
| Signing in issues a new session token and ends the one held before | `feature-review` and the person who reviews the increment |
| When a session ends, its authentication token is refused from that moment | `feature-review` and the person who reviews the increment |
| Disabling or deleting a user ends every session it holds | `feature-review` and the person who reviews the increment |
| After a factor is changed or removed, every other session of that user ends | `feature-review` and the person who reviews the increment |
| Every page and screen that requires signing in has a visible way to sign out | `feature-review` |
| An administrator can end the sessions of one user, or of every user of their customer | `feature-review` |
| A person can see and end the active sessions of their user | `feature-review` |
| Changing anything used to recover an account requires signing in again in full | `feature-review` and the person who reviews the increment |
| Every access decision is made by a back end service | `feature-review` and the person who reviews the increment |
| A self-contained token is trusted only after its signature or its MAC is checked | `feature-review` and the person who reviews the increment |
| A self-contained token is made and checked only with an algorithm the cryptographic inventory lists for its kind, never `none` | `feature-review` and the person who reviews the increment |
| The key that checks a self-contained token comes only from a source configured in advance for its issuer | `feature-review` and the person who reviews the increment |
| A self-contained token is accepted only within its validity period | `feature-review` and the person who reviews the increment |
| A token is accepted only for the use its type is meant for | `feature-review` and the person who reviews the increment |
| A service accepts a token only where the token is meant for it | `feature-review` and the person who reviews the increment |
| A token signed for more than one audience names them, and an audience added while the product runs is checked first | `feature-review` and the person who reviews the increment |
| An authentication token's grants are taken into account in every access decision | `feature-review` |
| A user is identified from a token only by its issuer's own value, read with that issuer | `feature-review` and the person who reviews the increment |
| The identity service sends a browser back only to an address registered in advance, matched character for character | `feature-review` and the person who reviews the increment |
| An authorization code is exchanged once, and a second attempt refuses every token issued from it | `feature-review` and the person who reviews the increment |
| An authorization code lives at most 10 minutes | `feature-review` and the person who reviews the increment |
| The identity service allows only the grants each frontend and system caller needs, and never the implicit or password grant | `feature-review` and the person who reviews the increment |
| The identity service requires a proof key on every authorization code request, and refuses the plain method | `feature-review` and the person who reviews the increment |
| A system that registers itself without signing in is checked, and a person is warned and asked to consent | `feature-review` and the person who reviews the increment |
| Every request a system caller sends to the identity service carries its identifier and its secret | `feature-review` and the person who reviews the increment |
| The identity service grants each frontend and system caller only the scopes it needs | `feature-review` |
| A refresh token held by a frontend is bound to it or works once, and reuse refuses every refresh token of that sign-in | `feature-review` and the person who reviews the increment |
| A refresh token stops working when its session ends | `feature-review` and the person who reviews the increment |
| An authorization code or a sign-in assertion is accepted only for a sign-in the same browser started | `feature-review` and the person who reviews the increment |
| Where more than one identity provider can sign a person in, every response is checked for which one sent it | `feature-review` and the person who reviews the increment |
| An OpenID Connect sign-in assertion is accepted only with the nonce sent for that sign-in | `feature-review` and the person who reviews the increment |
| An identity provider's metadata is refused unless its issuer matches exactly | `feature-review` and the person who reviews the increment |
| An OpenID Connect sign-in assertion is accepted only where its audience is exactly the identity service's own identifier | `feature-review` and the person who reviews the increment |
| A sign-out sent by an identity provider's server is accepted only as a logout token of the logout type | `feature-review` and the person who reviews the increment |
| As an OpenID Connect provider, the identity service never allows the response type `token` | `feature-review` and the person who reviews the increment |
| As an OpenID Connect provider, the identity service ends a sign-in at a system's request only after confirming it | `feature-review` and the person who reviews the increment |
| The identity service grants nothing on a person's behalf without consent | `feature-review` |
| A person asked to consent is shown what is granted, to which application, and for how long | `feature-review` |
| A person can see, change and withdraw every consent they have given | `feature-review` |
| An operation never decodes a value the framework has already decoded | `feature-review` |
| A value received from outside never changes the structure of anything the product builds with it, and is stored as it was received, never encoded | `feature-review` and the person who reviews the increment |
| A value placed into a URL is encoded for its place, and an address from data is used as a link or a redirect only with a safe scheme | `feature-review` and the person who reviews the increment |
| HTML received from outside is sanitised by a well-known library before it is stored, and only what the sanitiser keeps is shown | `feature-review` and the person who reviews the increment |
| No code is built or run from data received from outside | `feature-review` and the person who reviews the increment |
| An SVG received from outside is served or shown only after everything but drawing elements and their attributes is removed | `feature-review` and the person who reviews the increment |
| Markdown, CSS, XSL, BBCode or similar content received from outside is sanitised before it is shown, or shown as plain text | `feature-review` and the person who reviews the increment |
| No template is built from data received from outside | `feature-review` and the person who reviews the increment |
| A format string is never taken from data received from outside | `feature-review` and the person who reviews the increment |
| The product runs no code that reads or writes memory without the runtime checking its bounds | `feature-review` and the person who reviews the increment |
| XML received from outside is read with document type definitions refused and external entities never resolved | `feature-review` and the person who reviews the increment |
| Data received from outside is never turned into an object of a type the data itself names, or by an insecure mechanism | `feature-review` and the person who reviews the increment |
| Every value the product receives from outside is checked against the rules stated for it before it is used | `feature-review` and the person who reviews the increment |
| A limited quantity is never used beyond its limit, including by requests that arrive at the same moment | `feature-review` |
| Every response with a body declares a Content-Type that matches the body, and a text or XML type names its character set | `feature-review` and the person who reviews the increment |
| An API never redirects a request from HTTP to HTTPS | `feature-review` and the person who reviews the increment |
| A header that a part of the product in front of an API adds is used only as that part set it | `feature-review` and the person who reviews the increment |
| A WebSocket connection is made only over TLS | `feature-review` and the person who reviews the increment |
| A WebSocket connection is accepted only when its Origin is one of the product's own frontends | `feature-review` and the person who reviews the increment |
| A WebSocket connection is signed in by the product's own session, or by a token that meets every rule this document states for a session | `feature-review` and the person who reviews the increment |
| A token that signs in a WebSocket connection is obtained through a request already signed in over HTTPS | `feature-review` and the person who reviews the increment |
| Every operation that accepts a file has a largest size it accepts, and a larger file is refused before it is stored | `feature-review` and the person who reviews the increment |
| A file is accepted only when its extension is one its operation accepts and its content is of the type that extension names | `feature-review` and the person who reviews the increment |
| An archive is checked against the largest unpacked size and the most files stated for its operation before it is unpacked | `feature-review` and the person who reviews the increment |
| A file received from outside is never stored where a web server serves files directly, and is never run | `feature-review` and the person who reviews the increment |
| The name and the path under which a file is stored are made by the product | `feature-review` and the person who reviews the increment |
| A file the product serves names its download in a Content-Disposition header, with a name the product has checked or made | `feature-review` and the person who reviews the increment |
| An API that only the product's own frontends and services call publishes its documentation in every environment except production, and an API a customer's system calls publishes it everywhere | `feature-review` and the person who reviews the increment |
| An endpoint that monitors the product is an operation like any other, and reaches only the callers its contract states | `feature-review` and the person who reviews the increment |
| A browser never shows a response from an API as a page, except the documentation page, and a file the product serves is sent as an attachment | `feature-review` and the person who reviews the increment |
| Every cookie the product writes is marked `Secure`, and its name begins with `__Host-` or `__Secure-` as its reach requires | `feature-review` and the person who reviews the increment |
| A cookie that script does not read is marked `HttpOnly`, and its value never reaches a response body | `feature-review` and the person who reviews the increment |
| Every response the product sends carries `Strict-Transport-Security` with a `max-age` of at least one year and `includeSubDomains` | `feature-review` and the person who reviews the increment |
| An API lets a page at another origin read its responses only when that origin is on the API's own list of frontends | `feature-review` and the person who reviews the increment |
| Every page the product serves carries a Content-Security-Policy that loads and connects to nothing beyond its own address and the APIs it calls | `feature-review` and the person who reviews the increment |
| The documentation page runs no script beyond its one inline start script, allowed by its hash | `feature-review` and the person who reviews the increment |
| Every response that is not a page carries a Content-Security-Policy that allows nothing to load and no page to frame it | `feature-review` and the person who reviews the increment |
| Every response the product sends carries `X-Content-Type-Options: nosniff` | `feature-review` and the person who reviews the increment |
| Every page the product serves sets its referrer policy to `no-referrer` | `feature-review` and the person who reviews the increment |
| Every response the product sends carries a Content-Security-Policy whose `frame-ancestors` is `'none'` | `feature-review` and the person who reviews the increment |
| An operation that a cookie signs in refuses a request that did not come from one of the product's own frontends | `feature-review` and the person who reviews the increment |
| A message received through `postMessage` is used only when its origin and its content are valid, and any other is discarded | `feature-review` and the person who reviews the increment |
| A client certificate is checked as trusted before its identity signs a caller in or decides what it may do | `feature-review` and the person who reviews the increment |
| A frontend's address answers plain HTTP only by redirecting to HTTPS, and an API's address never answers plain HTTP | `feature-review` and the person who reviews the increment |
| Before the product sends anything over a connection, it checks the certificate it received, and every signature it makes or accepts in a handshake uses only what Appendix C of ASVS 5.0.0 approves, except a certificate's own signature | `feature-review` and the person who reviews the increment |
| A provider's service never turns off or replaces the check of the certificate it receives | `feature-review` and the person who reviews the increment |
| A TURN service the product runs relays only to addresses not reserved for a special purpose | `feature-review` and the person who reviews the increment |
| A media server the product runs checks the authentication of every SRTP packet, and drops a packet that fails | `feature-review` and the person who reviews the increment |
| A media server the product runs keeps processing media when it receives a malformed SRTP packet | `feature-review` and the person who reviews the increment |
| A signaling server the product runs limits how many messages each caller may send in a period | `feature-review` and the person who reviews the increment |
| A signaling server the product runs keeps processing legitimate messages when it receives a malformed one | `feature-review` and the person who reviews the increment |
| No value at either protection level, and no secret, is written to a log in any form | `feature-review` and the person who reviews the increment |
| The product records each of the stated events as a security event | `feature-review` and the person who reviews the increment |
| A value written into a log can never begin a new line or change another field of a line | `feature-review` and the person who reviews the increment |
| `Docs/AcceptedVulnerabilities.md` is changed in the same increment as what it lists | `feature-review` and the person who reviews the increment |

## What nothing catches

**Eight rules have no enforcer at all.** Nothing on the build sees them, and reading the
increment does not show them either.

- **A credential given to infrastructure that is not registered and authorised, or that
  still works after the infrastructure is no longer in use.** Registering infrastructure and
  retiring it are done by people on an environment, and no plan or increment shows either.
  Section 2 states the rule.
- **A secret of a deployed environment on a developer's machine.** Copying a secret to a
  machine happens outside any increment. Section 3 states the rule.
- **A secret stored encrypted by a key in the vault, not replaced when it may have been
  exposed.** Whether it may have been exposed is known only when it happens, and no plan or
  increment shows it. Section 3 states the rule.
- **A secret not replaced at once when it may have been exposed, or when a person who could
  reach it stops being a named administrator.** Both happen at a moment no plan or increment
  shows. Section 3 states the rule.
- **A credential given to a system caller that is not registered and authorised, or that
  still works after the caller is no longer in use.** Registering a system caller and
  retiring it are done by people, and no plan or increment shows either. Section 4 states
  the rule.
- **Every third-party library the product uses is brought to a current version at least
  once every 12 months, planned as work of its own.** Bringing a library current is planned
  as work of its own, apart from any Feature, so no plan or increment shows it happening.
  Section 13 states the rule.
- **A secret found in the repository, not replaced at once.** Whether a secret may have been
  exposed is known only when it happens, and no plan or increment shows it. Section 14
  states the rule.
- **The credential a part of the product uses to reach the vault, kept somewhere other than
  that environment, or not replaced on time.** A value a person sets on an environment has
  no recorded expected value to be compared against, and no plan or increment shows a person
  setting it. Section 14 states the rule.

