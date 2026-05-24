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
  * File overview: src/EmulationServer.Game/Data/Dbc/Characters/ChrRaceDbcRecord.cs
  * Documents the ChrRaceDbcRecord source file in the DBC loading and strongly typed client data records area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Game.Data.Dbc.Characters;

/**
  * Represents one ChrRaces.dbc row used for race validation and character display setup.
  * Positional fields carried by this record: Id, Flags, FactionId, ExplorationSoundId, MaleDisplayId, FemaleDisplayId, ClientPrefix, Speed, BaseLanguage, CreatureType, LoginEffect, ResSicknessSpellId, SplashSoundEntryId, StartingTaxiMask, ClientFileString, CinematicSequenceId, Name, FacialHairCustomization1, FacialHairCustomization2, HairCustomization.
  */
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
