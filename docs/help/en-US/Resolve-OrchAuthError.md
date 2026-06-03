---
document type: cmdlet
external help file: UiPathOrch.dll-Help.xml
HelpUri: 'https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Resolve-OrchAuthError.md'
Locale: en-US
Module Name: UiPathOrch
ms.date: 05/20/2026
PlatyPS schema version: 2024-05-01
title: Resolve-OrchAuthError
---

# Resolve-OrchAuthError

## SYNOPSIS

Diagnoses a UiPath Identity sign-in failure from the browser URL it left open.

## SYNTAX

### __AllParameterSets

```
Resolve-OrchAuthError [-Url] <string[]> [<CommonParameters>]
```

## ALIASES

## DESCRIPTION

When a PKCE / browser sign-in fails, UiPathOrch's local listener only observes that no authorization code arrived — the actual reason is on the page UiPath Identity redirected the browser to. That page's URL carries the failure detail, but it is encoded: either as a numeric or symbolic `errorCode` plus a percent-encoded `returnUrl` (and a `traceId`), or as a base64-encoded `errorId` JSON envelope.

`Resolve-OrchAuthError` takes that full browser URL and decodes it locally — no Orchestrator or Identity connection is made. It extracts the `errorCode`, the `traceId` (the server-side correlation id UiPath Identity engineering needs for opaque errors), the External Application / `client_id` and `redirect_uri` actually sent, and the requested scopes, then returns a plain-language diagnosis and a recommended action.

Recognized cases include `#219` ("user has not accepted the invitation" — the org-scoped Cloud Identity-URL regression fixed in 1.4.2), `invalid_request` with an invalid `redirect_uri` (a config `RedirectUrl` that is not registered on the external app — common with shared / pre-created apps), and `invalid_scope`. Unrecognized Identity errors are reported as server-side with the `traceId` to forward.

This cmdlet does not require a connected drive and is safe to run on any input; an unparseable or non-error URL yields a diagnosis explaining that, not an error.

Primary Endpoint: (none)

OAuth required scopes: (none)

Required permissions: (none)

## EXAMPLES

### Example 1: Diagnose a #219 sign-in failure

```powershell
PS C:\> Resolve-OrchAuthError 'https://cloud.uipath.com/identity_/web/login?errorCode=219&returnUrl=%2Fidentity_%2Fconnect%2Fauthorize%2Fcallback%3Fclient_id%3D820f076b-c773-4d0f-bc97-289cb5234572&traceId=0HNLC91R48MO0%3A000015F1'
```

Reports that `#219` is the org-scoped Cloud Identity-URL regression, recommends upgrading UiPathOrch to 1.4.2, and surfaces the `client_id` in use and the `traceId` to forward if the upgrade does not resolve it.

### Example 2: Diagnose an invalid redirect_uri

```powershell
PS C:\> Resolve-OrchAuthError 'https://cloud.uipath.com/identity_/web/?errorCode=invalid_request&errorId=<base64>'
```

Decodes the base64 `errorId` envelope and reports that the `redirect_uri` UiPathOrch sent is not registered on the external application, with the recommended action to align the config `RedirectUrl` with the app (or vice versa).

### Example 3: Diagnose the URL from the error message

```powershell
PS Orch1:\> try { Get-OrchPSDrive } catch { }
PS Orch1:\> Resolve-OrchAuthError 'https://cloud.uipath.com/identity_/web/login?errorCode=invalid_scope&returnUrl=%2Fidentity_%2Fconnect%2Fauthorize%3Fscope%3DOR.Administration%2520DataFabric.Data.Read'
```

When a sign-in fails, the thrown error message points here. Pasting the browser URL returns the requested scopes and the recommended action to reconcile the config `Scope` with what the external app grants.

## PARAMETERS

### -Url

The full browser URL from the failed sign-in — the address bar of the tab UiPath Identity left open.
Accepts one or more URLs and pipeline input.

**Wrap the URL in single quotes.** These URLs contain `&` (and sometimes `$`), which PowerShell would otherwise treat as an operator / a variable, so an unquoted URL fails to parse or is silently truncated at the first `&`. Single quotes pass it through literally; double quotes are not enough, because they still expand `$`.

```yaml
Type: System.String[]
DefaultValue: None
SupportsWildcards: false
Aliases: []
ParameterSets:
- Name: (All)
  Position: 0
  IsRequired: true
  ValueFromPipeline: true
  ValueFromPipelineByPropertyName: true
  ValueFromRemainingArguments: false
DontShow: false
AcceptedValues: []
HelpMessage: 'The full browser URL from the failed sign-in (the address bar of the tab Identity left open).'
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable,
-ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](https://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

You can pipe one or more error URLs to this cmdlet.

## OUTPUTS

### UiPath.PowerShell.Commands.OrchAuthErrorDiagnosis

An object with the decoded `ErrorCode`, `Error`, `ErrorDescription`, `TraceId`, `ClientId`, `RedirectUri`, `Scopes`, a plain-language `Diagnosis`, and a `RecommendedAction`.

## NOTES

No network call is made — decoding is entirely local. Nothing from the URL (including the authorization `code`, if present) is logged or transmitted.

## RELATED LINKS

[Import-OrchConfig](https://github.com/UiPath-Services/UiPathOrch/blob/master/docs/help/en-US/Import-OrchConfig.md)
