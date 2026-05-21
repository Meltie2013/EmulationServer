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
  * File overview: tools/EmulationServer.Tools.Extraction/Formats/Vmaps/Conversion/VmapVector3.cs
  * This file contains a small immutable vector model used by the vmap converter.
  * The converter stores positions, rotations, and bounds without taking a dependency on a rendering or physics library.
  */

namespace EmulationServer.Tools.Extraction.Formats.Vmaps.Conversion;

/**
  * Stores a three-component floating point vector from WMO and ADT source files.
  * The same model is used for geometry vertices, placement positions, rotations, and bounding boxes.
  */
public readonly record struct VmapVector3(float X, float Y, float Z)
{
    /**
      * Represents the zero vector used when optional source data is missing.
      */
    public static VmapVector3 Zero { get; } = new(0.0f, 0.0f, 0.0f);
}
