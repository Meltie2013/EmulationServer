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

namespace EmulationServer.Database.Accounts;

/**
  * Centralizes the RBAC permission identifiers used by database rows and command handlers.
  * Command ids start at 200 and are kept in alphabetical command syntax order so the C# ids match the seed SQL.
  */
public static class RbacPermissionIds
{
    public const uint AdministratorPermission = 190;
    public const uint GameMasterPermission = 191;
    public const uint PlayerPermission = 192;

    public const uint AdministratorCommands = 195;
    public const uint GameMasterCommands = 196;
    public const uint PlayerCommands = 197;

    public const uint CommandAccount = 200;
    public const uint CommandAccountCreate = 201;
    public const uint CommandAccountDelete = 202;
    public const uint CommandAccountRemovePermission = 203;
    public const uint CommandAccountSetPermission = 204;
    public const uint CommandBan = 205;
    public const uint CommandBanAccount = 206;
    public const uint CommandHelp = 207;
    public const uint CommandMap = 208;
    public const uint CommandMapInfo = 209;
    public const uint CommandMapRestart = 210;
    public const uint CommandMapShutdown = 211;
    public const uint CommandMapStart = 212;
    public const uint CommandReload = 213;
    public const uint CommandReloadRbac = 214;
    public const uint CommandServer = 215;
    public const uint CommandServerRestart = 216;
    public const uint CommandServerShutdown = 217;
    public const uint CommandBank = 218;
}
