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

using EmulationServer.Tools.Extraction.Client;


/**
 * File overview: tests/EmulationServer.Tests/Tools/ClientBuildsTests.cs
 * Documents the ClientBuildsTests source file in the automated test coverage for server behavior and data helpers area of the Emulation Server project.
 * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
 */

namespace EmulationServer.Tests.Tools;

/**
 * Owns the client builds tests behavior for the automated test coverage for server behavior and data helpers layer.
 * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
 */
public sealed class ClientBuildsTests
{
    /**
     * Determines whether supported returns true for expected extractor builds for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     * Inputs used by this operation: build.
     */
    [Theory]
    [InlineData(5875)]
    [InlineData(6005)]
    [InlineData(6141)]
    [InlineData(8606)]
    [InlineData(12340)]
    public void IsSupported_ReturnsTrue_ForExpectedExtractorBuilds(ushort build)
    {
        Assert.True(ClientBuilds.IsSupported(build));
    }

    /**
     * Requires throws for unsupported build for the automated test coverage for server behavior and data helpers workflow.
     * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
     */
    [Fact]
    public void Require_Throws_ForUnsupportedBuild()
    {
        Assert.Throws<NotSupportedException>(() => ClientBuilds.Require(15595));
    }
}
