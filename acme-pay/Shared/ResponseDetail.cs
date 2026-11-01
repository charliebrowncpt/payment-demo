namespace AcmePay.Shared;

// Code "00" is approved. On errors this carries the reason (E01, E02, E09, E29, E50).
public record ResponseDetail(
    string Code,                    // max 3 on payment responses ("PEND"/"COMPL" are the EFT exceptions)
    string? Source = null,          // Acquirer | EFT | 3DSecure | Application
    string? Description = null,     // max 50
    string? ClientMessage = null
);  // max 100, safe to show a cardholder