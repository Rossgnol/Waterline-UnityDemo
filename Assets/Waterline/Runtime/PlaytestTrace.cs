using System;
using System.Collections.Generic;

namespace Waterline
{
    // Pure data accounting. Scene sampling and disk writes live in HarborPlaytestRecorder.
    public sealed class PlaytestTrace
    {
        public struct Sample
        {
            public double seconds;
            public string stage, room, mode;
            public int attempt;
            public bool map, journal, debug;
            public float x, z;
        }

        [Serializable] public sealed class Totals
        {
            public string id;
            public double activeSeconds, horizontalMetres;
            public int entries, retryArrivals, mapOpens, journalOpens;
        }

        [Serializable] public sealed class Event
        {
            public double seconds;
            public string kind, from, to, reason;
            public int attempt;
        }

        [Serializable] public sealed class Report
        {
            public int schemaVersion = 1;
            public string sessionId, startedUtc, updatedUtc, source, scene, buildGuid, unityVersion;
            public string status = "in_progress";
            public string method = "Local sampled observations, not a usability verdict. Active time and horizontal distance require consecutive playing samples in the same attempt. UI, focus and retry boundaries are excluded. Stage totals include all attempts; event times are seconds since session start. Interactive source is not proof of human input.";
            public bool debugUsed;
            public double elapsedSeconds, activeSeconds, horizontalMetres;
            public int mapOpens, journalOpens, retries, captures, movementDiscontinuities, droppedEvents;
            public List<Totals> stages = new List<Totals>();
            public List<Totals> rooms = new List<Totals>();
            public List<Event> events = new List<Event>();
        }

        public Report Data { get; private set; }
        public bool Finished { get; private set; }
        private readonly Dictionary<string, Totals> stages = new Dictionary<string, Totals>();
        private readonly Dictionary<string, Totals> rooms = new Dictionary<string, Totals>();
        private Sample previous;
        private bool sampled;
        private const int EventLimit = 10000;

        public PlaytestTrace(Report report) { Data = report; }

        private Totals Get(Dictionary<string, Totals> lookup, List<Totals> list, string id)
        {
            if (!lookup.TryGetValue(id, out var value))
            {
                value = new Totals { id = id };
                lookup.Add(id, value); list.Add(value);
            }
            return value;
        }

        private void Add(Sample s, string kind, string from, string to, string reason = "")
        {
            if (Data.events.Count >= EventLimit) { Data.droppedEvents++; return; }
            Data.events.Add(new Event { seconds = s.seconds, kind = kind, from = from, to = to, reason = reason, attempt = s.attempt });
        }

        public void Observe(Sample s)
        {
            if (Finished) return;
            if (double.IsNaN(s.seconds) || double.IsInfinity(s.seconds) || s.seconds < 0 || (sampled && s.seconds < previous.seconds))
                throw new ArgumentException("Playtest samples require finite, monotonic session time.");
            s.stage = s.stage ?? "unknown"; s.room = s.room ?? "unmapped"; s.mode = s.mode ?? "paused";
            var stage = Get(stages, Data.stages, s.stage);
            var room = Get(rooms, Data.rooms, s.room);
            bool retry = sampled && s.attempt != previous.attempt;
            Data.elapsedSeconds = s.seconds;
            Data.debugUsed |= s.debug;

            if (sampled && !retry && previous.mode == "playing" && s.mode == "playing")
            {
                double dt = s.seconds - previous.seconds;
                var oldStage = stages[previous.stage]; var oldRoom = rooms[previous.room];
                Data.activeSeconds += dt; oldStage.activeSeconds += dt; oldRoom.activeSeconds += dt;
                double dx = s.x - previous.x, dz = s.z - previous.z;
                double distance = Math.Sqrt(dx * dx + dz * dz);
                // The controller's top speed is below 10 m/s. Reject external pose jumps.
                if (!double.IsNaN(distance) && !double.IsInfinity(distance) && distance <= 10 * dt + .25)
                {
                    Data.horizontalMetres += distance; oldStage.horizontalMetres += distance; oldRoom.horizontalMetres += distance;
                }
                else { Data.movementDiscontinuities++; Add(s, "movement_gap", previous.room, s.room, "implausible_displacement"); }
            }

            if (retry)
            {
                Data.retries += Math.Max(1, s.attempt - previous.attempt);
                Add(s, "retry", previous.room, s.room, "checkpoint_restore");
            }
            if (!sampled || previous.stage != s.stage || retry)
            {
                stage.entries++; Add(s, "stage", sampled ? previous.stage : "", s.stage, retry ? "retry" : "progress");
            }
            if (!sampled || previous.room != s.room || retry)
            {
                string reason = retry ? "retry" : sampled ? "sampled_transition" : "spawn";
                if (sampled) Add(s, "room_exit", previous.room, s.room, reason);
                if (retry) room.retryArrivals++; else room.entries++;
                Add(s, "room_enter", sampled ? previous.room : "", s.room, reason);
            }
            if (!sampled || previous.mode != s.mode) Add(s, "mode", sampled ? previous.mode : "", s.mode);
            if (s.mode == "caught" && (!sampled || previous.mode != "caught")) Data.captures++;
            if (s.map && (!sampled || !previous.map))
            { Data.mapOpens++; stage.mapOpens++; room.mapOpens++; Add(s, "map_open", s.room, s.stage); }
            if (s.journal && (!sampled || !previous.journal))
            { Data.journalOpens++; stage.journalOpens++; room.journalOpens++; Add(s, "journal_open", s.room, s.stage); }
            previous = s; sampled = true;
        }

        public void Finish(string status)
        {
            if (Finished) return;
            Finished = true; Data.status = status;
            if (sampled)
            {
                Add(previous, "room_exit", previous.room, "", "session_end");
                Add(previous, "session_end", "", status);
            }
        }
    }
}
