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

namespace EmulationServer.RealmServer.Auth;

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

    public static string GetVerificationSummary()
    {
        return string.Join(", ", CriticalOpCodes.Select(definition => $"{definition.Name}=0x{definition.ExpectedValue:X2}"));
    }

    private readonly record struct RealmAuthOpcodeDefinition(string Name, RealmAuthOpCode OpCode, byte ExpectedValue)
    {
        public byte ActualValue => (byte)OpCode;
    }
}
