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

using System.Collections.Concurrent;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

/**
  * File overview: src/EmulationServer.Network/Networking/Sessions/SessionManager.cs
  * This file belongs to the network session lifecycle and packet dispatch portion of the Emulation Server project.
  * The comments in this file describe ownership, lifecycle, validation, and protocol responsibilities so future contributors can understand the code before changing it.
  */

namespace EmulationServer.Network.Networking.Sessions;

/**
  * Represents the session manager component in the network session lifecycle and packet dispatch area.
  * It coordinates a collection of related runtime objects and keeps ownership rules in one place.
  */
public sealed class SessionManager
{
    private readonly ConcurrentDictionary<Guid, SessionEntry> _sessions = new();

    /**
      * Gets or stores the count value used by SessionManager.
      * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
      */
    public int Count => _sessions.Count;

    /**
      * Attempts the operation without treating a normal failure as an exceptional condition.
      * The method is part of SessionManager and keeps this workflow isolated from the caller.
      * The boolean result lets callers branch without throwing for normal negative outcomes.
      */
    public bool TryAddSession(RealmSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        SessionEntry entry = new(session);

        return _sessions.TryAdd(session.Id, entry);
    }

    /**
      * Performs the complete session operation for SessionManager.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      */
    public void CompleteSession(RealmSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_sessions.TryRemove(session.Id, out SessionEntry? entry))
        {
            entry.MarkCompleted();
        }
    }

    /**
      * Performs the disconnect all async operation for SessionManager.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      */
    public Task DisconnectAllAsync()
    {
        Task[] disconnectTasks = _sessions.Values
            .Select(entry => entry.Session.DisconnectAsync())
            .ToArray();

        return Task.WhenAll(disconnectTasks);
    }

    /**
      * Performs the wait for all sessions async operation for SessionManager.
      * Keeping this logic in a dedicated method makes the control flow easier to read and test.
      * The asynchronous shape allows shutdown cancellation and network/file operations to avoid blocking the server loop.
      * The cancellation token lets server shutdown stop the operation without leaving partial runtime work behind.
      */
    public async Task WaitForAllSessionsAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Task[] completionTasks = _sessions.Values
            .Select(entry => entry.Completion)
            .ToArray();

        if (completionTasks.Length == 0)
        {
            return;
        }

        Task allSessionsStopped = Task.WhenAll(completionTasks);
        Task timeoutTask = Task.Delay(timeout, cancellationToken);

        Task completedTask = await Task.WhenAny(allSessionsStopped, timeoutTask);

        if (completedTask == allSessionsStopped)
        {
            await allSessionsStopped;
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            Logger.Write(LogType.WARNING, "Stopped waiting for sessions because shutdown wait was cancelled.", nameof(SessionManager));
            return;
        }

        Logger.Write(LogType.WARNING, $"Stopped waiting for sessions because shutdown wait timed out after {timeout.TotalSeconds:0.##} second(s).",
            nameof(SessionManager));
    }

    /**
      * Represents the session entry component in the network session lifecycle and packet dispatch area.
      * The type keeps related data and behavior together so the rest of the project can depend on a clear responsibility boundary.
      */
    private sealed class SessionEntry
    {
        /**
          * Stores the completion dependency or runtime value for SessionEntry.
          * The field is kept private so all updates can be controlled through the owning type and its synchronization rules.
          */
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /**
          * Creates a new SessionEntry instance and stores the dependencies required by the component.
          * Constructor validation happens here so invalid dependencies fail during startup instead of later in the runtime loop.
          */
        public SessionEntry(RealmSession session)
        {
            Session = session;
        }

        /**
          * Gets or stores the session value used by SessionEntry.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public RealmSession Session { get; }

        /**
          * Gets or stores the completion value used by SessionEntry.
          * Keeping the value exposed through a property makes configuration, snapshots, and protocol models easier to inspect without exposing unrelated implementation details.
          */
        public Task Completion => _completion.Task;

        /**
          * Performs the mark completed operation for SessionEntry.
          * Keeping this logic in a dedicated method makes the control flow easier to read and test.
          */
        public void MarkCompleted()
        {
            _completion.TrySetResult();
        }
    }
}
