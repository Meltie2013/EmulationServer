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
// File: tests/EmulationServer.Tests/RealmServer/RealmAuthOpcodeVerifierTests.cs
// Purpose: Contains realm auth opcode verifier tests code for the realm server authentication, realm-list, and account connection layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using EmulationServer.RealmServer.Auth;

namespace EmulationServer.Tests.RealmServer;

// Type: RealmAuthOpcodeVerifierTests
// Purpose: Provides realm auth opcode verifier tests behavior for the realm server authentication, realm-list, and account connection layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class RealmAuthOpcodeVerifierTests
{

    [Fact]
    // Method: VerifyCriticalOpCodes_ShouldPass_WhenRealmAuthOpCodesMatchExpectedClientProtocolValues
    // Purpose: Executes the verify critical op codes should pass when realm auth op codes match expected client protocol values operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmAuthOpcodeVerifierTests so callers do not duplicate validation, protocol, or persistence rules.
    public void VerifyCriticalOpCodes_ShouldPass_WhenRealmAuthOpCodesMatchExpectedClientProtocolValues()
    {
        RealmAuthOpcodeVerifier.VerifyCriticalOpCodes();
    }

    [Fact]
    // Method: CriticalRealmAuthOpCodes_ShouldUseExpectedClientProtocolValues
    // Purpose: Executes the critical realm auth op codes should use expected client protocol values operation for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmAuthOpcodeVerifierTests so callers do not duplicate validation, protocol, or persistence rules.
    public void CriticalRealmAuthOpCodes_ShouldUseExpectedClientProtocolValues()
    {
        Assert.Equal((byte)0x00, (byte)RealmAuthOpCode.AuthLogonChallenge);
        Assert.Equal((byte)0x01, (byte)RealmAuthOpCode.AuthLogonProof);
        Assert.Equal((byte)0x02, (byte)RealmAuthOpCode.AuthReconnectChallenge);
        Assert.Equal((byte)0x03, (byte)RealmAuthOpCode.AuthReconnectProof);
        Assert.Equal((byte)0x10, (byte)RealmAuthOpCode.RealmList);
    }

    [Fact]
    // Method: GetVerificationSummary_ShouldIncludeCriticalRealmAuthOpCodes
    // Purpose: Retrieves get verification summary should include critical realm auth op codes data for the realm server authentication, realm-list, and account connection layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to RealmAuthOpcodeVerifierTests so callers do not duplicate validation, protocol, or persistence rules.
    public void GetVerificationSummary_ShouldIncludeCriticalRealmAuthOpCodes()
    {
        string summary = RealmAuthOpcodeVerifier.GetVerificationSummary();

        Assert.Contains("AUTH_LOGON_CHALLENGE=0x00", summary);
        Assert.Contains("AUTH_LOGON_PROOF=0x01", summary);
        Assert.Contains("AUTH_RECONNECT_CHALLENGE=0x02", summary);
        Assert.Contains("AUTH_RECONNECT_PROOF=0x03", summary);
        Assert.Contains("REALM_LIST=0x10", summary);
    }
}
