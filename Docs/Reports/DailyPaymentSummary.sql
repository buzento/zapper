-- The daily payment summary: for one South African day, the count and value of approved payments, declined
-- payments, reversals and refunds, and beside them the payments still pending, failed or unresolved, for each
-- sub-merchant and currency. Run it signed in as ZapperDataReaderRole. Change only the date below; nothing else.
WITH "SummaryDay" AS (
-- The South African day to summarise, as YYYY-MM-DD. Change only this date.
SELECT DATE '2026-01-01' AS "Day"
),
-- the day runs from 00:00 to 24:00 at UTC+02:00, which is 22:00 UTC the day before; AT TIME ZONE '+02' is never used,
-- because PostgreSQL reads it as two hours west of UTC
"DayBounds" AS (
    SELECT ("Day"::timestamp - INTERVAL '2 hours') AT TIME ZONE 'UTC' AS "StartsAt",
           (("Day"::timestamp - INTERVAL '2 hours') AT TIME ZONE 'UTC') + INTERVAL '1 day' AS "EndsAt"
    FROM "SummaryDay"
),
-- one row for each fact that counts on the day: the sub-merchant and currency it counts for, its column, and its
-- amount in the lowest unit
"Fact" AS (
    -- an approval counts on the day of Acme's time, whatever the payment's status is now, at the authorised amount;
    -- Acme's outcome is read from the changes, since it is set once and every change is recorded
    SELECT payment."SubMerchantId", payment."CurrencyTypeId", 'Approved' AS "Column", payment."AuthorisedAmount" AS "Amount"
    FROM "Payment"."Payment" AS payment
    CROSS JOIN "DayBounds" AS bounds
    WHERE payment."AcmeTransactionAt" >= bounds."StartsAt"
      AND payment."AcmeTransactionAt" < bounds."EndsAt"
      AND EXISTS (
          SELECT 1
          FROM "Payment"."PaymentChange" AS change
          JOIN "Payment"."PaymentStateType" AS state ON state."Id" = change."ToPaymentStateTypeId"
          WHERE change."PaymentId" = payment."Id"
            AND state."Name" = 'Approved')

    UNION ALL

    -- a decline counts the same way, at the amount requested
    SELECT payment."SubMerchantId", payment."CurrencyTypeId", 'Declined', payment."Amount"
    FROM "Payment"."Payment" AS payment
    CROSS JOIN "DayBounds" AS bounds
    WHERE payment."AcmeTransactionAt" >= bounds."StartsAt"
      AND payment."AcmeTransactionAt" < bounds."EndsAt"
      AND EXISTS (
          SELECT 1
          FROM "Payment"."PaymentChange" AS change
          JOIN "Payment"."PaymentStateType" AS state ON state."Id" = change."ToPaymentStateTypeId"
          WHERE change."PaymentId" = payment."Id"
            AND state."Name" = 'Declined')

    UNION ALL

    -- a reversal or a refund counts on the day Acme Switch processed it, at its own amount, only once it is applied
    SELECT payment."SubMerchantId", payment."CurrencyTypeId", eventType."Name", event."Amount"
    FROM "Payment"."PaymentEvent" AS event
    JOIN "Payment"."PaymentEventType" AS eventType ON eventType."Id" = event."PaymentEventTypeId"
    JOIN "Payment"."PaymentEventStateType" AS eventState ON eventState."Id" = event."PaymentEventStateTypeId"
    JOIN "Payment"."Payment" AS payment ON payment."Id" = event."PaymentId"
    CROSS JOIN "DayBounds" AS bounds
    WHERE event."AcmeProcessedAt" >= bounds."StartsAt"
      AND event."AcmeProcessedAt" < bounds."EndsAt"
      AND eventType."Name" IN ('Reversal', 'Refund')
      AND eventState."Name" = 'Applied'

    UNION ALL

    -- a payment now pending, failed or unresolved counts on the day it was recorded, at the amount requested;
    -- Recorded and OutcomeUnknown both show as Pending
    SELECT payment."SubMerchantId",
           payment."CurrencyTypeId",
           CASE state."Name"
               WHEN 'Failed' THEN 'Failed'
               WHEN 'Unresolved' THEN 'Unresolved'
               ELSE 'Pending'
           END,
           payment."Amount"
    FROM "Payment"."Payment" AS payment
    JOIN "Payment"."PaymentStateType" AS state ON state."Id" = payment."PaymentStateTypeId"
    CROSS JOIN "DayBounds" AS bounds
    WHERE payment."RecordedAt" >= bounds."StartsAt"
      AND payment."RecordedAt" < bounds."EndsAt"
      AND state."Name" IN ('Recorded', 'OutcomeUnknown', 'Failed', 'Unresolved')
)
-- only a sub-merchant and currency with a fact on the day has a row; a value is the lowest unit divided by 100, with
-- exactly two decimal places
SELECT subMerchant."MerchantReference" AS "SubMerchantReference",
       subMerchant."TradingName" AS "TradingName",
       currency."Code" AS "CurrencyCode",
       COUNT(*) FILTER (WHERE fact."Column" = 'Approved') AS "ApprovedCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Approved'), 0)::numeric / 100)::numeric(20, 2) AS "ApprovedValue",
       COUNT(*) FILTER (WHERE fact."Column" = 'Declined') AS "DeclinedCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Declined'), 0)::numeric / 100)::numeric(20, 2) AS "DeclinedValue",
       COUNT(*) FILTER (WHERE fact."Column" = 'Reversal') AS "ReversalCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Reversal'), 0)::numeric / 100)::numeric(20, 2) AS "ReversalValue",
       COUNT(*) FILTER (WHERE fact."Column" = 'Refund') AS "RefundCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Refund'), 0)::numeric / 100)::numeric(20, 2) AS "RefundValue",
       COUNT(*) FILTER (WHERE fact."Column" = 'Pending') AS "PendingCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Pending'), 0)::numeric / 100)::numeric(20, 2) AS "PendingValue",
       COUNT(*) FILTER (WHERE fact."Column" = 'Failed') AS "FailedCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Failed'), 0)::numeric / 100)::numeric(20, 2) AS "FailedValue",
       COUNT(*) FILTER (WHERE fact."Column" = 'Unresolved') AS "UnresolvedCount",
       (COALESCE(SUM(fact."Amount") FILTER (WHERE fact."Column" = 'Unresolved'), 0)::numeric / 100)::numeric(20, 2) AS "UnresolvedValue"
FROM "Fact" AS fact
JOIN "Merchant"."SubMerchant" AS subMerchant ON subMerchant."Id" = fact."SubMerchantId"
JOIN "Payment"."CurrencyType" AS currency ON currency."Id" = fact."CurrencyTypeId"
GROUP BY subMerchant."MerchantReference", subMerchant."TradingName", currency."Code"
ORDER BY "SubMerchantReference", "CurrencyCode";
