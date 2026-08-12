# Run 002 execution-integrity invalidation

Status: **INVALID / ENDED — RUNTIME ENDPOINT CORRESPONDENCE FAILURE**

This notice does not delete or rewrite any Run 002 raw evidence. It corrects the adjudicative status after a cross-tab concurrency audit discovered that the measured Run 002 clients did not have exclusive correspondence to the frozen runtime declared by `fea71ed342fb6c7f3e9202e5aba2fbe2110ca346`.

## Frozen claim that cannot be sustained

Run 002 froze implementation:

```text
2d81186040268c101497555f5a9425d0eba18ce2
```

The Run 002 L0/L1/Program Host wrappers all connected to the canonical named pipe:

```text
\\.\pipe\shelleye-dev
```

However, a separate prospective Repair 2 diagnostic proxy from another active Build 002 tab had already acquired that same pipe before Run 002 began. That proxy forwarded byte-for-byte to:

```text
\\.\pipe\shelleye-repair2-target
```

whose target kernel was repeatedly replaced during prospective Repair 2 preflight. Therefore the Run 002 client results cannot prove that they exercised frozen implementation `2d811860...`.

## Durable evidence

The complete proxy metadata log is preserved next to this notice as:

```text
cross-tab-proxy-rpc.log
```

Source log SHA-256 at preservation:

```text
DC7B0FE6684E555B102F432A1AA8CE7731D5FA8D1DF54A246CD35ADCD29ADF83
```

Source log size:

```text
426570 bytes
```

The log records:

```text
2026-08-12T13:21:46.738Z LISTEN
2026-08-12T13:33:43.878Z CLIENT
2026-08-12T13:38:19.618Z CLIENT
2026-08-12T13:38:49.424Z CLIENT
```

The corresponding authenticated owner scheduled-task runs were:

```text
Run 002 L0 Owner:
2026-08-12T09:33:43-04:00, result 0, user StealthEye

Run 002 L1 PID Stress:
2026-08-12T09:38:19-04:00, result 0, user StealthEye

Run 002 Program Host:
2026-08-12T09:38:49-04:00, result 1, user StealthEye
```

The proxy log also records the Run 002 L1 client issuing its `world.providers`, `provider.probe`, and repeated `process.start` / `process.wait` RPCs through the proxy during the measured interval.

## Adjudication

The raw Run 002 facts remain historically true as observations of *some* owner-context Build 002 candidate runtime:

- the 256-iteration PID stress returned success;
- the Program Host invocation returned nonzero with `native_error: statx(fd) failed.`

But neither result is valid evidence for or against the frozen Run 002 implementation because the endpoint-runtime identity was not controlled.

Therefore:

```text
Run 002 acceptance status: INVALID / ENDED
Run 002 product classification: NONE
reason: execution-integrity / frozen-runtime correspondence failure
```

No Run 002 result may be counted toward final Build 002 acceptance. A subsequent run requires a fresh prospective source candidate, exclusive canonical pipe ownership, fresh owner-context L0, and a new freeze before L1.
