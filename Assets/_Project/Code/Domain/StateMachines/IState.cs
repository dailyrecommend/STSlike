namespace STSlike.Domain.StateMachines
{
    /// <summary>
    /// One state in a <see cref="StateMachine"/>. Enter runs when the machine
    /// switches to it; Exit runs when it switches away. A state triggers its
    /// own transitions by calling ChangeState on the machine it was given.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Exit();
    }
}
