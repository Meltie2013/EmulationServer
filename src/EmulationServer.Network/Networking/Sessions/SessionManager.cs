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
  * Documents the SessionManager source file in the internal server networking, packet framing, and peer/session lifecycle area of the Emulation Server project.
  * The notes below explain intent, ownership, validation rules, and protocol/data responsibilities using normal comments instead of XML documentation.
  */

namespace EmulationServer.Network.Networking.Sessions;

/**
  * Owns the session manager behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
  * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
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
      * Performs the complete session operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: session.
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
      * Performs the disconnect all operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
      */
    public Task DisconnectAllAsync()
    {
        Task[] disconnectTasks = _sessions.Values
            .Select(entry => entry.Session.DisconnectAsync())
            .ToArray();

        return Task.WhenAll(disconnectTasks);
    }

    /**
      * Performs the wait for all sessions operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
      * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
      * Inputs used by this operation: timeout, cancellationToken.
      * The asynchronous form keeps network, file, and database work from blocking the main server loop and allows cancellation during shutdown.
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
            Logger.Write(LogType.WARNING, "Stopped waiting for sessions because shutdown wait was cancelled.", "SessionManager");
            return;
        }

        Logger.Write(LogType.WARNING, $"Stopped waiting for sessions because shutdown wait timed out after {timeout.TotalSeconds:0.##} second(s).",
            "SessionManager");
    }

    /**
      * Owns the session entry behavior for the internal server networking, packet framing, and peer/session lifecycle layer.
      * The class keeps related validation, state changes, and external calls in one place so startup, runtime handling, and shutdown remain predictable.
      */
    private sealed class SessionEntry
    {
        /**
          * Holds the private completion state used by the owning component.
          * The field is intentionally kept behind the type boundary so updates can follow the component lifecycle and synchronization rules.
          */
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /**
          * Performs the session entry operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
          * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
          * Inputs used by this operation: session.
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
          * Performs the mark completed operation for the internal server networking, packet framing, and peer/session lifecycle workflow.
          * Keeping this logic in a dedicated method makes the control flow easier to review, test, and adjust without spreading protocol or data rules across the codebase.
          */
        public void MarkCompleted()
        {
            _completion.TrySetResult();
        }
    }
}
