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

namespace EmulationServer.Network.Networking.Sessions;

public sealed class SessionManager
{
    private readonly ConcurrentDictionary<Guid, SessionEntry> _sessions = new();

    public int Count => _sessions.Count;

    public bool TryAddSession(RealmSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        SessionEntry entry = new(session);

        return _sessions.TryAdd(session.Id, entry);
    }

    public void CompleteSession(RealmSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (_sessions.TryRemove(session.Id, out SessionEntry? entry))
        {
            entry.MarkCompleted();
        }
    }

    public Task DisconnectAllAsync()
    {
        Task[] disconnectTasks = _sessions.Values
            .Select(entry => entry.Session.DisconnectAsync())
            .ToArray();

        return Task.WhenAll(disconnectTasks);
    }

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

    private sealed class SessionEntry
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SessionEntry(RealmSession session)
        {
            Session = session;
        }

        public RealmSession Session { get; }

        public Task Completion => _completion.Task;

        public void MarkCompleted()
        {
            _completion.TrySetResult();
        }
    }
}
