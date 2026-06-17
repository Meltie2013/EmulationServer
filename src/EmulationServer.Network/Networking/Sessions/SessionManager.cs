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
// File: src/EmulationServer.Network/Networking/Sessions/SessionManager.cs
// Purpose: Contains session manager code for the packet serialization, socket transport, and protocol framing layer.
// Documentation: Uses normal line comments so the source stays readable without C# XML documentation tags.

using System.Collections.Concurrent;

using EmulationServer.Shared.Logging;
using EmulationServer.Shared.Logging.Enums;

namespace EmulationServer.Network.Networking.Sessions;

// Type: SessionManager
// Purpose: Provides session manager behavior for the packet serialization, socket transport, and protocol framing layer.
// Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
public sealed class SessionManager
{
    private readonly ConcurrentDictionary<Guid, SessionEntry> _sessions = new();

    // Property: Gets or sets the count value used by the packet serialization, socket transport, and protocol framing layer.
    // Value: count value exposed by the owning type.
    public int Count => _sessions.Count;

    // Method: TryAddSession
    // Purpose: Executes the try add session operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // Returns: Returns true when try add session succeeds or the requested condition is met; otherwise returns false.
    // Notes: This keeps the operation scoped to SessionManager so callers do not duplicate validation, protocol, or persistence rules.
    public bool TryAddSession(RealmSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        SessionEntry entry = new(session);

        return _sessions.TryAdd(session.Id, entry);
    }

    // Method: CompleteSession
    // Purpose: Executes the complete session operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - session: Session value supplied by the caller for this operation.
    // Returns: none.
    // Notes: This keeps the operation scoped to SessionManager so callers do not duplicate validation, protocol, or persistence rules.
    public void CompleteSession(RealmSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_sessions.TryRemove(session.Id, out SessionEntry? entry))
        {
            entry.MarkCompleted();
        }
    }

    // Method: DisconnectAllAsync
    // Purpose: Executes the disconnect all operation for the packet serialization, socket transport, and protocol framing layer.
    // Parameters: none.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to SessionManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
    public Task DisconnectAllAsync()
    {
        Task[] disconnectTasks = _sessions.Values
            .Select(entry => entry.Session.DisconnectAsync())
            .ToArray();

        return Task.WhenAll(disconnectTasks);
    }

    // Method: WaitForAllSessionsAsync
    // Purpose: Handles wait for all sessions work for the packet serialization, socket transport, and protocol framing layer.
    // Parameters:
    // - timeout: Timeout value supplied by the caller for this operation.
    // - cancellationToken: Token used to cancel the operation during shutdown or caller-requested aborts.
    // Returns: Returns an asynchronous operation that completes when the requested work has finished.
    // Notes: This keeps the operation scoped to SessionManager so callers do not duplicate validation, protocol, or persistence rules.
    // Notes: The asynchronous form avoids blocking server loops and supports cooperative shutdown when a cancellation token is supplied.
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

    // Type: SessionEntry
    // Purpose: Provides session entry behavior for the packet serialization, socket transport, and protocol framing layer.
    // Notes: Keep protocol, database, and lifecycle changes inside this boundary unless a shared abstraction is intentionally introduced.
    private sealed class SessionEntry
    {

        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Constructor: SessionEntry
        // Purpose: Initializes a new SessionEntry instance with dependencies and values required by the packet serialization, socket transport, and protocol framing layer.
        // Parameters:
        // - session: Session value supplied by the caller for this operation.
        // Returns: none.
        // Notes: This keeps the operation scoped to SessionEntry so callers do not duplicate validation, protocol, or persistence rules.
        public SessionEntry(RealmSession session)
        {
            Session = session;
        }

        // Property: Gets or sets the session value used by the packet serialization, socket transport, and protocol framing layer.
        // Value: session value exposed by the owning type.
        public RealmSession Session { get; }

        // Property: Gets or sets the completion value used by the packet serialization, socket transport, and protocol framing layer.
        // Value: completion value exposed by the owning type.
        public Task Completion => _completion.Task;

        // Method: MarkCompleted
        // Purpose: Executes the mark completed operation for the packet serialization, socket transport, and protocol framing layer.
        // Parameters: none.
        // Returns: none.
        // Notes: This keeps the operation scoped to SessionEntry so callers do not duplicate validation, protocol, or persistence rules.
        public void MarkCompleted()
        {
            _completion.TrySetResult();
        }
    }
}
