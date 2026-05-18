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

using EmulationServer.RealmServer.Auth;

/**
  * File overview: tests/EmulationServer.Tests/RealmServer/RealmAuthOpcodeVerifierTests.cs
  * This file belongs to the project runtime logic and supporting data models portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Tests.RealmServer;

/**
  * Represents the realm auth opcode verifier tests component in the project runtime logic and supporting data models area.
  * It documents expected behavior with automated assertions so regressions are easier to detect.
  */
public sealed class RealmAuthOpcodeVerifierTests
{
    [Fact]
    /**
      * Verifies that loaded data satisfies the expected format and consistency rules.
      * The method is part of RealmAuthOpcodeVerifierTests and keeps this workflow isolated from the caller.
      */
    public void VerifyCriticalOpCodes_ShouldPass_WhenRealmAuthOpCodesMatchExpectedClientProtocolValues()
    {
        RealmAuthOpcodeVerifier.VerifyCriticalOpCodes();
    }

    [Fact]
    /**
      * Performs the critical realm auth op codes should use expected client protocol values operation for RealmAuthOpcodeVerifierTests.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public void CriticalRealmAuthOpCodes_ShouldUseExpectedClientProtocolValues()
    {
        Assert.Equal((byte)0x00, (byte)RealmAuthOpCode.AuthLogonChallenge);
        Assert.Equal((byte)0x01, (byte)RealmAuthOpCode.AuthLogonProof);
        Assert.Equal((byte)0x02, (byte)RealmAuthOpCode.AuthReconnectChallenge);
        Assert.Equal((byte)0x03, (byte)RealmAuthOpCode.AuthReconnectProof);
        Assert.Equal((byte)0x10, (byte)RealmAuthOpCode.RealmList);
    }

    [Fact]
    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmAuthOpcodeVerifierTests and keeps this workflow isolated from the caller.
      */
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
