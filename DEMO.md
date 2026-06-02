# CardTrader Demo Walkthrough

CardTrader is a proof-of-concept trading card application built to demonstrate
relationship-based access control. Every permission check — who can view a trade,
who can cancel it, whether a parent can act on a child's behalf — is evaluated by
a dedicated authorization layer rather than scattered `if` statements in application
code.

This walkthrough tours the key scenarios in roughly 15 minutes using only the
browser UI. No tooling required beyond a running instance.

**Starting the app:**
```powershell
.\Start-Dev.ps1
```
Then open `http://localhost:5057` in your browser.

---

## Accounts used in this demo

| Handle | Email | Password | Role |
|--------|-------|----------|------|
| Admin  | admin@cardtrader.local | Admin123! | Pre-seeded admin |
| Alice  | alice@demo.local | Demo1234! | Register during setup |
| Bob    | bob@demo.local | Demo1234! | Register during setup |
| Parent | parent@demo.local | Demo1234! | Register for delegation scene |
| Child  | child@demo.local | Demo1234! | Register for delegation scene |

Register Alice, Bob, Parent, and Child via **Register** before starting the scenes.
The admin account is created automatically on first run.

---

## Scene 1 — The basic trade lifecycle

**Goal:** Show that the initiator can cancel their own proposal, and the recipient
(and only the recipient) can accept it.

1. **Log in as Alice.** Go to **Cards** and note the available card list.
2. Go to **Rosters**, create a roster called *Alice's Collection*, and mint a card
   into it (choose any card, print number 1).
3. Go to **Trades**, click **New Trade**, select Bob as the recipient, and choose
   the card instance you just minted. Submit.
4. **Open a second browser window (or incognito).** Log in as Bob.
5. Bob's **Trades** page shows the incoming proposal. Bob clicks **Accept**.
   The trade status changes to *Accepted* and the card instance now belongs to Bob.

**What to observe:** Alice cannot accept her own proposal — the Accept button is
absent for her. Bob cannot cancel the proposal — that button is absent for him.
These are not UI-only restrictions; the service layer enforces the same rules and
will reject unauthorized calls regardless of how they arrive.

---

## Scene 2 — A stranger cannot touch trades they are not party to

**Goal:** Show that an unrelated user cannot view or manipulate another user's
trade.

1. Register (or log in as) a third account — use **Eve** (`eve@demo.local`).
2. Alice creates a new trade proposal to Bob (repeat Scene 1 steps 1-3).
3. Log in as Eve. Go to **Trades**.

**What to observe:** The trade between Alice and Bob does not appear in Eve's list.
Eve has no relationship to that proposal — not initiator, recipient, supervisor, or
facilitator — so the authorization layer returns no access and the application
surfaces nothing.

---

## Scene 3 — Admin override

**Goal:** Show that an admin can cancel any trade, bypassing the normal
initiator-only restriction, without needing a personal relationship to either party.

1. Alice creates a new trade proposal to Bob (they should each have no prior
   relationship to each other for clarity).
2. Log in as **admin@cardtrader.local**.
3. Go to **Admin**. The pending trade appears in the admin panel.
4. Click **Cancel** on the trade.

**What to observe:** The trade is cancelled even though the admin is neither the
initiator nor the recipient and has no FGA tuple connecting them to this proposal.
The admin bypass is implemented via an Identity role check that runs *before* the
FGA check, not by adding admin to every tuple in the system. Removing the Admin
role from the account immediately removes this capability with no tuple cleanup
required.

---

## Scene 4 — Delegation: a parent supervising a child's account

**Goal:** Show that a delegated supervisor can view and cancel a child's trade, but
cannot accept it on the child's behalf.

1. Log in as **Child**. Mint a card instance into a new roster, then go to
   **Delegations** and create a delegation to **Parent**. Activate it.
2. Child creates a trade proposal (Child initiates, Bob as recipient).
3. Log in as **Parent**. Go to **Trades**.

**What to observe:**
- Parent can *see* Child's proposal. The delegation writes a `supervisor` tuple on
  the trade, granting `can_view` and `can_cancel` to Parent.
- Parent can click **Cancel** on Child's trade. The action succeeds.
- Parent does **not** see an **Accept** button. The `supervisor` relation maps to
  `can_cancel` only — supervisory oversight does not grant the ability to complete
  a deal on someone's behalf.

**Bonus — revoke delegation mid-trade:**

1. Child creates another trade proposal without cancelling it.
2. Log in as Child, go to **Delegations**, and deactivate the delegation to Parent.
3. Log back in as Parent and refresh **Trades**.

**What to observe:** Parent's view of Child's trade disappears immediately. There is
no grace period — the moment the delegation is deactivated, the FGA check returns
deny and the application shows nothing.

---

## Scene 5 — Roster visibility and card sharing

**Goal:** Show that card instance visibility flows through roster membership, and
that revoking roster access closes visibility.

1. Log in as **Alice**. Create a roster and mint several card instances into it.
2. Go to the roster settings and make it **Public** (visible to all users).
3. Log in as **Eve**. Browse to Alice's roster.

**What to observe:** Eve can see all card instances in the public roster. She cannot
trade any of them — she can view but `can_trade` requires ownership.

4. Log back in as Alice and make the roster **Private**.
5. Refresh Eve's browser.

**What to observe:** The roster and its cards are no longer visible to Eve. Making
a roster private deletes the `user:* viewer` tuple; since Eve had no other path to
the cards, access is revoked immediately.

---

## What this demonstrates

| Scenario | Authorization property shown |
|----------|------------------------------|
| Scene 1 | Roles are per-object and per-relationship, not global flags |
| Scene 2 | Default-deny: no relationship means no access, no exceptions |
| Scene 3 | RBAC and ReBAC can coexist; admin bypass is additive, not a model change |
| Scene 4 | Delegated authority is scoped (view+cancel) and instantly revocable |
| Scene 5 | Visibility propagates through object graphs; revocation is immediate |

The authorization model lives entirely in `authz/model.fga`. No application code
contains permission logic — the service layer asks the authorization layer for a
yes/no decision and acts on the answer.
