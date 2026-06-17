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
// File: src/EmulationServer.Game/Data/Dbc/Characters/ChrRaceDbcRecord.cs
// Purpose: Contains chr race DBC record code for the game-domain data, player state, DBC, and world-template layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Game.Data.Dbc.Characters;

// Type: ChrRaceDbcRecord
// Purpose: Represents chr race DBC record data passed through the game-domain data, player state, DBC, and world-template layer.
// Constructor values:
// - Id: ID identifier used to select the exact record, object, or runtime owner.
// - Flags: Flags value supplied by the caller for this operation.
// - FactionId: Faction ID identifier used to select the exact record, object, or runtime owner.
// - ExplorationSoundId: Exploration sound ID identifier used to select the exact record, object, or runtime owner.
// - MaleDisplayId: Male display ID identifier used to select the exact record, object, or runtime owner.
// - FemaleDisplayId: Female display ID identifier used to select the exact record, object, or runtime owner.
// - ClientPrefix: Client prefix value supplied by the caller for this operation.
// - Speed: Speed value supplied by the caller for this operation.
// - BaseLanguage: Base language value supplied by the caller for this operation.
// - CreatureType: Creature type value supplied by the caller for this operation.
// - LoginEffect: Login effect value supplied by the caller for this operation.
// - ResSicknessSpellId: Res sickness spell ID identifier used to select the exact record, object, or runtime owner.
// - SplashSoundEntryId: Splash sound entry ID identifier used to select the exact record, object, or runtime owner.
// - StartingTaxiMask: Starting taxi mask value supplied by the caller for this operation.
// - ClientFileString: Client file string value supplied by the caller for this operation.
// - CinematicSequenceId: Cinematic sequence ID identifier used to select the exact record, object, or runtime owner.
// - Name: Name value supplied by the caller for this operation.
// - FacialHairCustomization1: Facial hair customization1 value supplied by the caller for this operation.
// - FacialHairCustomization2: Facial hair customization2 value supplied by the caller for this operation.
// - HairCustomization: Hair customization value supplied by the caller for this operation.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed record ChrRaceDbcRecord(
    int Id,
    int Flags,
    int FactionId,
    int ExplorationSoundId,
    int MaleDisplayId,
    int FemaleDisplayId,
    string ClientPrefix,
    float Speed,
    int BaseLanguage,
    int CreatureType,
    int LoginEffect,
    int ResSicknessSpellId,
    int SplashSoundEntryId,
    int StartingTaxiMask,
    string ClientFileString,
    int CinematicSequenceId,
    string Name,
    string FacialHairCustomization1,
    string FacialHairCustomization2,
    string HairCustomization);
