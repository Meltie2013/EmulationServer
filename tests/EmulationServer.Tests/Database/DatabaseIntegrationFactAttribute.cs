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
// File: tests/EmulationServer.Tests/Database/DatabaseIntegrationFactAttribute.cs
// Purpose: Contains database integration fact attribute code for the automated test and verification layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Tests.Database;

[AttributeUsage(AttributeTargets.Method)]
// Type: DatabaseIntegrationFactAttribute
// Purpose: Provides database integration fact attribute behavior for the automated test and verification layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class DatabaseIntegrationFactAttribute : FactAttribute
{

    // Constant: Defines the enabled value constant used by the automated test and verification layer.
    // Value: fixed enabled value value used anywhere this rule or protocol value is needed.
    private const string EnabledValue = "true";

    // Constant: Defines the environment variable name constant used by the automated test and verification layer.
    // Value: fixed environment variable name value used anywhere this rule or protocol value is needed.
    private const string EnvironmentVariableName = "EMULATIONSERVER_RUN_DATABASE_TESTS";

    // Constructor: DatabaseIntegrationFactAttribute
    // Purpose: Initializes a new DatabaseIntegrationFactAttribute instance with dependencies and values required by the automated test and verification layer.
    // Parameters: none.
    // Returns: none.
    // Notes: This keeps the operation scoped to DatabaseIntegrationFactAttribute so callers do not duplicate validation, protocol, or persistence rules.
    public DatabaseIntegrationFactAttribute()
    {
        string? enabled = Environment.GetEnvironmentVariable(EnvironmentVariableName);

        if (!string.Equals(enabled, EnabledValue, StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Set {EnvironmentVariableName}=true to run database integration tests.";
        }
    }
}
