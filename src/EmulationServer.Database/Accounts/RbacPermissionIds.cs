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
// File: src/EmulationServer.Database/Accounts/RbacPermissionIds.cs
// Purpose: Contains RBAC permission ids code for the database persistence, repository, and MySQL connectivity layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

namespace EmulationServer.Database.Accounts;

// Type: RbacPermissionIds
// Purpose: Provides RBAC permission ids behavior for the database persistence, repository, and MySQL connectivity layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public static class RbacPermissionIds
{
    // Constant: Defines the administrator permission constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed administrator permission value used anywhere this rule or protocol value is needed.
    public const uint AdministratorPermission = 190;
    // Constant: Defines the game master permission constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed game master permission value used anywhere this rule or protocol value is needed.
    public const uint GameMasterPermission = 191;
    // Constant: Defines the player permission constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed player permission value used anywhere this rule or protocol value is needed.
    public const uint PlayerPermission = 192;

    // Constant: Defines the administrator commands constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed administrator commands value used anywhere this rule or protocol value is needed.
    public const uint AdministratorCommands = 195;
    // Constant: Defines the game master commands constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed game master commands value used anywhere this rule or protocol value is needed.
    public const uint GameMasterCommands = 196;
    // Constant: Defines the player commands constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed player commands value used anywhere this rule or protocol value is needed.
    public const uint PlayerCommands = 197;

    // Constant: Defines the command account constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command account value used anywhere this rule or protocol value is needed.
    public const uint CommandAccount = 200;
    // Constant: Defines the command account create constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command account create value used anywhere this rule or protocol value is needed.
    public const uint CommandAccountCreate = 201;
    // Constant: Defines the command account delete constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command account delete value used anywhere this rule or protocol value is needed.
    public const uint CommandAccountDelete = 202;
    // Constant: Defines the command account remove permission constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command account remove permission value used anywhere this rule or protocol value is needed.
    public const uint CommandAccountRemovePermission = 203;
    // Constant: Defines the command account set permission constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command account set permission value used anywhere this rule or protocol value is needed.
    public const uint CommandAccountSetPermission = 204;
    // Constant: Defines the command ban constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command ban value used anywhere this rule or protocol value is needed.
    public const uint CommandBan = 205;
    // Constant: Defines the command ban account constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command ban account value used anywhere this rule or protocol value is needed.
    public const uint CommandBanAccount = 206;
    // Constant: Defines the command help constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command help value used anywhere this rule or protocol value is needed.
    public const uint CommandHelp = 207;
    // Constant: Defines the command map constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command map value used anywhere this rule or protocol value is needed.
    public const uint CommandMap = 208;
    // Constant: Defines the command map info constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command map info value used anywhere this rule or protocol value is needed.
    public const uint CommandMapInfo = 209;
    // Constant: Defines the command map restart constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command map restart value used anywhere this rule or protocol value is needed.
    public const uint CommandMapRestart = 210;
    // Constant: Defines the command map shutdown constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command map shutdown value used anywhere this rule or protocol value is needed.
    public const uint CommandMapShutdown = 211;
    // Constant: Defines the command map start constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command map start value used anywhere this rule or protocol value is needed.
    public const uint CommandMapStart = 212;
    // Constant: Defines the command reload constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command reload value used anywhere this rule or protocol value is needed.
    public const uint CommandReload = 213;
    // Constant: Defines the command reload RBAC constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command reload RBAC value used anywhere this rule or protocol value is needed.
    public const uint CommandReloadRbac = 214;
    // Constant: Defines the command server constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command server value used anywhere this rule or protocol value is needed.
    public const uint CommandServer = 215;
    // Constant: Defines the command server restart constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command server restart value used anywhere this rule or protocol value is needed.
    public const uint CommandServerRestart = 216;
    // Constant: Defines the command server shutdown constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command server shutdown value used anywhere this rule or protocol value is needed.
    public const uint CommandServerShutdown = 217;
    // Constant: Defines the command bank constant used by the database persistence, repository, and MySQL connectivity layer.
    // Value: fixed command bank value used anywhere this rule or protocol value is needed.
    public const uint CommandBank = 218;
}
