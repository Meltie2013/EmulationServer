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

/**
  * File overview: src/RealmServer/Auth/RealmAuthOpcodeVerifier.cs
  * This file belongs to the realm authentication, build validation, and realm list packet creation portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.RealmServer.Auth;

/**
  * Represents the realm auth opcode verifier component in the realm authentication, build validation, and realm list packet creation area.
  * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
  */
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

    /**
      * Verifies that loaded data satisfies the expected format and consistency rules.
      * The method is part of RealmAuthOpcodeVerifier and keeps this workflow isolated from the caller.
      */
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

    /**
      * Returns the current value or snapshot without exposing mutable internal state.
      * The method is part of RealmAuthOpcodeVerifier and keeps this workflow isolated from the caller.
      */
    public static string GetVerificationSummary()
    {
        return string.Join(", ", CriticalOpCodes.Select(definition => $"{definition.Name}=0x{definition.ExpectedValue:X2}"));
    }

    /**
      * Represents immutable struct data passed between parts of the server.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
      */
    private readonly record struct RealmAuthOpcodeDefinition(string Name, RealmAuthOpCode OpCode, byte ExpectedValue)
    {
        /**
          * Gets or stores the actual value value used by struct.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public byte ActualValue => (byte)OpCode;
    }
}
