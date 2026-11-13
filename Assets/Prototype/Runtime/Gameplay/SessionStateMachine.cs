using System;

namespace ClearPath
{
    public enum SessionState { Ready, Playing, Won, Lost }
    public sealed class SessionStateMachine
    {
        public SessionState State { get; private set; } = SessionState.Ready;
        public event Action<SessionState> Changed;
        public void Reset() { State = SessionState.Ready; Changed?.Invoke(State); }
        public bool Start()
        {
            if (State != SessionState.Ready) return false;
            State = SessionState.Playing; Changed?.Invoke(State); return true;
        }
        public void Finish(bool won)
        {
            if (State != SessionState.Playing) return;
            State = won ? SessionState.Won : SessionState.Lost; Changed?.Invoke(State);
        }
    }
}
