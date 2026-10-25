#!/usr/bin/env bash
# Walks every case in the brief against the running stack, through our API, its worker,
# the real Acme client, the stub and real TLS, and fails when a case does not end in its
# expected status. Plan Zapper_Team_Lead_Assement, Feature "End to End validation checks".
# It calls API endpoints only and never reads the database. Run it from the repository
# root, after `docker compose up -d --build --wait`. It needs bash, curl and jq.
#
# Written for the bash macOS ships as well as a newer one, so it uses no associative array.

set -u

# The seeded data, as README.md lists it.
sub_merchant=ZAP1000001
customer_approve=CUS-APPROVE
customer_decline_05=CUS-DECLINE-05
customer_decline_51=CUS-DECLINE-51
customer_decline_54=CUS-DECLINE-54
customer_advice_twice=CUS-ADVICE-TWICE
customer_reversal_first=CUS-REVERSAL-FIRST
customer_late_answer=CUS-LATE-ANSWER
customer_never_received=CUS-NEVER-RECEIVED
customer_gateway_error_once=CUS-GATEWAY-ERROR-ONCE

amount=1999
currency=ZAR
api=https://localhost:8443
stub=https://localhost:${ACME_STUB_PORT:-8444}
ca=Code/Certificates/ca.crt
routes_key=Code/Certificates/AcmeStubRoutes.key
poll_seconds=2
case_seconds=120
health_seconds=120

cd "$(dirname "$0")/.." || exit 1

missing=""
for program in curl jq; do
  if ! command -v "$program" >/dev/null 2>&1; then
    missing="$missing $program"
  fi
done
if [ -n "$missing" ]; then
  echo "Not installed:$missing. Install it and run the script again."
  exit 1
fi
for file in "$ca" "$routes_key"; do
  if [ ! -f "$file" ]; then
    echo "$file is missing. Run docker compose up -d --build --wait from the repository root first."
    exit 1
  fi
done

umask 077
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
# the key goes in a file curl reads, so it never shows in the list of running processes
printf 'Authorization: Bearer %s\n' "$(cat "$routes_key")" > "$work/stub-authorization"

# Sends one request, and waits out a 429 within the deadline it is given.
# Sets response_status, "000" when curl could not make the call, response_body and
# response_error.
request() {
  local method=$1 url=$2 body=$3 deadline=$4 authorization=$5
  local arguments retry_after
  while true; do
    arguments=(--silent --show-error --cacert "$ca" --request "$method"
      --output "$work/body" --dump-header "$work/headers" --write-out '%{http_code}')
    if [ -n "$body" ]; then
      arguments+=(--header 'Content-Type: application/json' --data "$body")
    fi
    if [ -n "$authorization" ]; then
      arguments+=(--header "@$authorization")
    fi
    : > "$work/body"
    : > "$work/headers"
    response_status=$(curl "${arguments[@]}" "$url" 2> "$work/error")
    if [ $? -ne 0 ]; then
      response_status=000
    fi
    response_body=$(cat "$work/body")
    response_error=$(cat "$work/error")
    if [ "$response_status" != 429 ]; then
      return
    fi
    retry_after=$(sed -n 's/^[Rr]etry-[Aa]fter: *\([0-9][0-9]*\).*/\1/p' "$work/headers" | head -n 1)
    if [ -z "$retry_after" ]; then
      retry_after=5
    fi
    if [ $(( $(date +%s) + retry_after )) -gt "$deadline" ]; then
      return
    fi
    sleep "$retry_after"
  done
}

describe_failure() {
  if [ "$response_status" = 000 ]; then
    echo "no answer: $response_error"
  else
    local code
    code=$(printf '%s' "$response_body" | jq -r '.code? // empty' 2>/dev/null)
    echo "HTTP $response_status${code:+, code $code}"
  fi
}

echo "Waiting for $api/v1/health"
health_deadline=$(( $(date +%s) + health_seconds ))
while true; do
  request GET "$api/v1/health" "" "$health_deadline" ""
  if [ "$response_status" = 200 ]; then
    break
  fi
  if [ "$(date +%s)" -ge "$health_deadline" ]; then
    echo "The API did not answer 200 within $health_seconds seconds. Last answer: $(describe_failure) $response_body"
    echo "Run docker compose up -d --build --wait, and read docker compose logs."
    exit 1
  fi
  sleep "$poll_seconds"
done

# The ten cases, each named by its scenario title.
titles=(
  ""
  "Payment with the approving card is approved"
  "Payments with the declining cards are declined with their codes"
  "Same payment submitted twice returns the first payment"
  "Payment Advice delivered twice leaves the payment approved"
  "Reversal that arrives before the Payment Advice leaves the payment reversed"
  "Approved payment reversed by Acme Switch shows as reversed"
  "Approved payment refunded in two parts shows as partially refunded then refunded"
  "Payment that Acme does not answer in time is approved after a query"
  "Payment Acme never received is sent again and approved"
  "Payment that meets a gateway error once is approved after a query"
)

# One entry per payment. Case 2 has three, one for each declining card.
count=0
add_payment() {
  payment_case[$count]=$1
  payment_customer[$count]=$2
  payment_code[$count]=$3
  payment_reference[$count]=$4
  payment_id[$count]=""
  payment_state[$count]=waiting
  payment_stage[$count]=0
  payment_reads[$count]=0
  payment_last[$count]=""
  payment_reason[$count]=""
  payment_recorded[$count]=0
  count=$(( count + 1 ))
}

start=$(date -u +%Y%m%d%H%M%S)
add_payment 1 "$customer_approve" "" "E2E-$start-1"
add_payment 2 "$customer_decline_05" 05 "E2E-$start-2-05"
add_payment 2 "$customer_decline_51" 51 "E2E-$start-2-51"
add_payment 2 "$customer_decline_54" 54 "E2E-$start-2-54"
add_payment 3 "$customer_approve" "" "E2E-$start-3"
add_payment 4 "$customer_advice_twice" "" "E2E-$start-4"
add_payment 5 "$customer_reversal_first" "" "E2E-$start-5"
add_payment 6 "$customer_approve" "" "E2E-$start-6"
add_payment 7 "$customer_approve" "" "E2E-$start-7"
add_payment 8 "$customer_late_answer" "" "E2E-$start-8"
add_payment 9 "$customer_never_received" "" "E2E-$start-9"
add_payment 10 "$customer_gateway_error_once" "" "E2E-$start-10"

# The status a payment must reach at its stage. Cases 6 and 7 pass through Approved, and
# the stub's routes then move them on.
expected_status() {
  case "$1:$2" in
    2:0) echo Declined ;;
    5:0) echo Reversed ;;
    6:1) echo Reversed ;;
    7:1) echo PartiallyRefunded ;;
    7:2) echo Refunded ;;
    *) echo Approved ;;
  esac
}

last_stage() {
  case "$1" in
    6) echo 1 ;;
    7) echo 2 ;;
    *) echo 0 ;;
  esac
}

# What else the payment must show once it reads its expected status. Prints what is
# wrong, or nothing.
check_payment() {
  local case_number=$1 stage=$2 code=$3 body=$4 filter=""
  case "$case_number:$stage" in
    1:0) filter='.responseCode == "00" and (.authorisedAmount | tostring) == "1999"' ;;
    2:0) filter=".responseCode == \"$code\"" ;;
    5:0) filter='(.authorisedAmount | tostring) == "1999" and (.reversedAmount | tostring) == "1999"' ;;
    6:1) filter='(.reversedAmount | tostring) == "1999" and (.refundedAmount | tostring) == "0"' ;;
    7:1) filter='(.refundedAmount | tostring) == "500"' ;;
    7:2) filter='(.refundedAmount | tostring) == "1999"' ;;
  esac
  if [ -n "$filter" ] && ! printf '%s' "$body" | jq -e "$filter" >/dev/null 2>&1; then
    printf '%s' "$body" | jq -r '"response code \(.responseCode), authorised amount \(.authorisedAmount), reversed total \(.reversedAmount), refunded total \(.refundedAmount)"'
  fi
}

fail_case() {
  local case_number=$1 reason=$2 j
  for (( j = 0; j < count; j++ )); do
    if [ "${payment_case[$j]}" = "$case_number" ] && [ "${payment_state[$j]}" = waiting ]; then
      payment_state[$j]=failed
    fi
  done
  payment_state[$3]=failed
  payment_reason[$3]=$reason
}

payment_body() {
  printf '{"customerReference":"%s","subMerchantReference":"%s","amount":%s,"currencyCode":"%s","appPaymentReference":"%s"}' \
    "$1" "$sub_merchant" "$amount" "$currency" "$2"
}

# Record first, then wait, so the slow cases wait at the same time.
for (( i = 0; i < count; i++ )); do
  payment_recorded[$i]=$(date +%s)
  deadline=$(( payment_recorded[i] + case_seconds ))
  body=$(payment_body "${payment_customer[$i]}" "${payment_reference[$i]}")
  request POST "$api/v1/payments" "$body" "$deadline" ""
  if [ "$response_status" != 201 ]; then
    fail_case "${payment_case[$i]}" "recording ${payment_reference[$i]} was refused: $(describe_failure)" "$i"
    continue
  fi
  payment_id[$i]=$(printf '%s' "$response_body" | jq -r '.id')
  echo "Recorded ${payment_reference[$i]} as ${payment_id[$i]}"
  if [ "${payment_case[$i]}" = 3 ]; then
    request POST "$api/v1/payments" "$body" "$deadline" ""
    if [ "$response_status" != 201 ]; then
      fail_case 3 "recording ${payment_reference[$i]} again was refused: $(describe_failure)" "$i"
      continue
    fi
    second_id=$(printf '%s' "$response_body" | jq -r '.id')
    if [ "$second_id" != "${payment_id[$i]}" ]; then
      fail_case 3 "recording ${payment_reference[$i]} again returned $second_id, not ${payment_id[$i]}" "$i"
      continue
    fi
    echo "Recorded ${payment_reference[$i]} again, and was given ${payment_id[$i]} again"
  fi
done

# Calls the stub's reversal or refund route, as Acme Switch would.
stub_action() {
  local index=$1 route=$2 amount_part=$3
  local deadline=$(( payment_recorded[index] + case_seconds ))
  request POST "$stub/stub/$route" "{\"TransactionID\":\"${payment_id[$index]}\"$amount_part}" "$deadline" "$work/stub-authorization"
  if [ "$response_status" != 200 ]; then
    fail_case "${payment_case[$index]}" "the stub's $route route gave $(describe_failure)" "$index"
    return 1
  fi
  echo "Sent ${payment_reference[$index]} to the stub's $route route"
}

waiting=1
while [ "$waiting" = 1 ]; do
  waiting=0
  for (( i = 0; i < count; i++ )); do
    if [ "${payment_state[$i]}" != waiting ]; then
      continue
    fi
    case_number=${payment_case[$i]}
    stage=${payment_stage[$i]}
    expected=$(expected_status "$case_number" "$stage")
    deadline=$(( payment_recorded[i] + case_seconds ))
    request GET "$api/v1/payments/${payment_id[$i]}" "" "$deadline" ""
    if [ "$response_status" != 200 ]; then
      fail_case "$case_number" "reading ${payment_reference[$i]} gave $(describe_failure)" "$i"
      continue
    fi
    status=$(printf '%s' "$response_body" | jq -r '.status')
    payment_last[$i]=$status
    payment_reads[$i]=$(( payment_reads[i] + 1 ))
    if [ "$case_number" = 8 ] && [ "${payment_reads[$i]}" = 1 ] && [ "$status" != Pending ]; then
      fail_case 8 "the first read of ${payment_reference[$i]} showed $status, not Pending" "$i"
      continue
    fi
    if [ "$status" = "$expected" ]; then
      problem=$(check_payment "$case_number" "$stage" "${payment_code[$i]}" "$response_body")
      if [ -n "$problem" ]; then
        fail_case "$case_number" "${payment_reference[$i]} reads $status with $problem" "$i"
        continue
      fi
      if [ "$stage" = "$(last_stage "$case_number")" ]; then
        payment_state[$i]=passed
        continue
      fi
      case "$case_number:$stage" in
        6:0) stub_action "$i" reversals "" || continue ;;
        7:0) stub_action "$i" refunds ',"Amount":500' || continue ;;
        7:1) stub_action "$i" refunds ',"Amount":1499' || continue ;;
      esac
      payment_stage[$i]=$(( stage + 1 ))
    elif [ "$status" != Pending ]; then
      fail_case "$case_number" "${payment_reference[$i]} reads $status, expected $expected" "$i"
      continue
    elif [ "$(date +%s)" -ge "$deadline" ]; then
      fail_case "$case_number" "${payment_reference[$i]} still reads $status $case_seconds seconds after it was recorded" "$i"
      continue
    fi
    waiting=1
  done
  if [ "$waiting" = 1 ]; then
    sleep "$poll_seconds"
  fi
done

all_passed=1
for (( i = 0; i < count; i++ )); do
  if [ "${payment_state[$i]}" != passed ]; then
    all_passed=0
  fi
done

# The read at the end: a notification that arrives after a case first passed must not
# change its status unseen.
if [ "$all_passed" = 1 ]; then
  for (( i = 0; i < count; i++ )); do
    case_number=${payment_case[$i]}
    expected=$(expected_status "$case_number" "$(last_stage "$case_number")")
    request GET "$api/v1/payments/${payment_id[$i]}" "" $(( $(date +%s) + case_seconds )) ""
    if [ "$response_status" != 200 ]; then
      fail_case "$case_number" "reading ${payment_reference[$i]} at the end gave $(describe_failure)" "$i"
      continue
    fi
    status=$(printf '%s' "$response_body" | jq -r '.status')
    if [ "$status" != "$expected" ]; then
      fail_case "$case_number" "${payment_reference[$i]} read $expected, and reads $status at the end" "$i"
    fi
    payment_last[$i]=$status
  done
fi

echo
exit_code=0
for (( c = 1; c <= 10; c++ )); do
  result=PASS
  read_statuses=""
  reasons=""
  for (( i = 0; i < count; i++ )); do
    if [ "${payment_case[$i]}" != "$c" ]; then
      continue
    fi
    if [ "${payment_state[$i]}" != passed ]; then
      result=FAIL
    fi
    read_statuses="$read_statuses${read_statuses:+, }${payment_last[$i]:-nothing}"
    if [ -n "${payment_reason[$i]}" ]; then
      reasons="$reasons    ${payment_reason[$i]}"$'\n'
    fi
  done
  if [ "$result" = FAIL ]; then
    exit_code=1
  fi
  echo "$result | ${titles[$c]} | expected $(expected_status "$c" "$(last_stage "$c")") | read $read_statuses"
  printf '%s' "$reasons"
done

exit "$exit_code"
