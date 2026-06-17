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
// File: src/EmulationServer.Shared/Configuration/ConfigurationException.cs
// Purpose: Contains configuration exception code for the shared infrastructure, logging, timing, and cross-service utility layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Shared.Configuration;

// Type: ConfigurationException
// Purpose: Provides configuration exception behavior for the shared infrastructure, logging, timing, and cross-service utility layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class ConfigurationException : Exception
{

    // Constructor: ConfigurationException
    // Purpose: Initializes a new ConfigurationException instance with dependencies and values required by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfigurationException so callers do not duplicate validation, protocol, or persistence rules.
    public ConfigurationException(string message) : base(message)
    {

    }

    // Constructor: ConfigurationException
    // Purpose: Initializes a new ConfigurationException instance with dependencies and values required by the shared infrastructure, logging, timing, and cross-service utility layer.
    // Parameters:
    // - message: Message value supplied by the caller for this operation.
    // - innerException: Inner exception value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to ConfigurationException so callers do not duplicate validation, protocol, or persistence rules.
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {

    }
}
