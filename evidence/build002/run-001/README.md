# Build 002 measured Run 001 — preserved failure

Status: **FAILED / ENDED — preserved prospectively**

Frozen acceptance commit: `dfb463d5426335e876a8f8bbac45e88da3a769c9`
Frozen source implementation: `5dff520bac3fe32a1e9ff9cee4e6b34e2a85ed8a`

Run 001 began with the frozen real-Linux PID-reuse stress under the authenticated interactive owner token `STEALTHEYELLC\StealthEye`, Session 1.

## L1 bounded PID stress

`l1-pid-reuse-stress.json` records:

- 256 real `/usr/bin/sleep 0.02` launch/wait iterations;
- PASS;
- observed PID reuse: 0 (best-effort pressure is allowed not to observe reuse);
- wrong rebound: 0;
- false Linux process rebounds: 0.

## Program Host measured invocation

The frozen `program-host/src/build002-acceptance.js` invocation was started under the same authenticated owner context and exited nonzero.

Observed failure:

```text
code: native_error
The 'Get-Process' command was found in the module 'Microsoft.PowerShell.Management',
but the module could not be loaded because the built-in PowerShell module was not
available from the embedded Core host's normal module path.
```

This is a genuine measured required-gate failure. The failed invocation is not erased or reclassified as a harness-only event.

Post-failure inspection found the frozen/published runtime contains the PowerShell SDK module manifests under:

```text
runtimes\win\lib\net10.0\Modules
```

but does not contain a root-level:

```text
Modules
```

The `Microsoft.PowerShell.Management.psd1` manifest is present in the nested SDK content location. Therefore the observed problem is a published embedded-PowerShell packaging/module-discovery defect. Correcting it changes the frozen candidate/runtime packaging contract and may not be done inside Run 001.

Run 001 is ended here. Any repair is prospective and requires a new candidate freeze and restart of the affected measured acceptance campaign.
