//
// Copyright (C) 2026 Emulation Server Project
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//

namespace EmulationServer.Game.Players;

/**
  * Carries a persisted inventory placement change for one item instance.
  */
public sealed record PlayerInventoryPlacementUpdate(
    uint ItemGuid,
    uint BagGuid,
    byte Slot);
