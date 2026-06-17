//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// File: src/RealmServer/Auth/RealmAuthOpcodeVerifier.cs
// Purpose: Contains realm auth opcode verifier code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.RealmServer.Auth;

// Type: RealmAuthOpcodeVerifier
// Purpose: Provides realm auth opcode verifier behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RealmAuthOpcodeVerifier
{

    private static readonly IReadOnlyList<RealmAuthOpcodeDefinition> CriticalOpCodes =
    [
        new("AUTH_LOGON_CHALLENGE", RealmAuthOpCode.AuthLogonChallenge, 0x00),
        new("AUTH_LOGON_PROOF", RealmAuthOpCode.AuthLogonProof, 0x01),
        new("AUTH_RECONNECT_CHALLENGE", RealmAuthOpCode.AuthReconnectChallenge, 0x02),
        new("AUTH_RECONNECT_PROOF", RealmAuthOpCode.AuthReconnectProof, 0x03),
        new("REALM_LIST", RealmAuthOpCode.RealmList, 0x10),
    ];

    // Method: VerifyCriticalOpCodes
    // Purpose: Executes the verify critical op codes operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmAuthOpcodeVerifier so callers do not duplicate validation, protocol, or persistence rules.
    public static void VerifyCriticalOpCodes()
    {
        List<string> errors = [];

        foreach (RealmAuthOpcodeDefinition definition in CriticalOpCodes)
        {
            byte actualValue = definition.ActualValue;

            if (actualValue != definition.ExpectedValue)
            {
                errors.Add($"{definition.Name} expected 0x{definition.ExpectedValue:X2} but was 0x{actualValue:X2}");
            }
        }

        foreach (IGrouping<byte, RealmAuthOpcodeDefinition> duplicateGroup in CriticalOpCodes.GroupBy(definition => definition.ActualValue).Where(group => group.Count() > 1))
        {
            string names = string.Join(", ", duplicateGroup.Select(definition => definition.Name));
            errors.Add($"Duplicate critical auth opcode value 0x{duplicateGroup.Key:X2} used by: {names}");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Critical RealmServer authentication opcode verification failed: {string.Join("; ", errors)}.");
        }
    }

    // Method: GetVerificationSummary
    // Purpose: Retrieves get verification summary data for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: Returns the string value produced by this operation.
    // Notes: This keeps the operation scoped to RealmAuthOpcodeVerifier so callers do not duplicate validation, protocol, or persistence rules.
    public static string GetVerificationSummary()
    {
        return string.Join(", ", CriticalOpCodes.Select(definition => $"{definition.Name}=0x{definition.ExpectedValue:X2}"));
    }

    // Type: RealmAuthOpcodeDefinition
    // Purpose: Represents realm auth opcode definition data passed through the realm server authentication, realm-list, and account connection layer.
    // Constructor values:
    // - Name: Name value supplied by the caller for this operation.
    // - OpCode: Op code value supplied by the caller for this operation.
    // - ExpectedValue: Expected value value supplied by the caller for this operation.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private readonly record struct RealmAuthOpcodeDefinition(string Name, RealmAuthOpCode OpCode, byte ExpectedValue)
    {

        // Method: ActualValue
        // Purpose: Executes the actual value operation for the realm server authentication, realm-list, and account connection layer.
        // Parameters: none.
        // Returns: Returns the byte value produced by this operation.
        // Notes: This keeps the operation scoped to RealmAuthOpcodeDefinition so callers do not duplicate validation, protocol, or persistence rules.
        public byte ActualValue => (byte)OpCode;
    }
}
