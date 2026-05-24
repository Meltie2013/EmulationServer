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
  * File overview: src/EmulationServer.Shared/Configuration/ConfigurationException.cs
  * Documents the ConfigurationException source file in the shared configuration, logging, and utility support area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Shared.Configuration;

/**
  * Owns the configuration exception behavior for the shared configuration, logging, and utility support layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
  */
public sealed class ConfigurationException : Exception
{
    /**
      * Initializes a new ConfigurationException instance with the dependencies required by the shared configuration, logging, and utility support workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      */
    public ConfigurationException(string message) : base(message)
    {

    }

    /**
      * Initializes a new ConfigurationException instance with the dependencies required by the shared configuration, logging, and utility support workflow.
      * Constructor validation is performed early so invalid settings fail during startup instead of surfacing later in the server loop.
      * Inputs used by this operation: message, innerException.
      */
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {

    }
}
