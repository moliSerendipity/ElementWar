public abstract class CharacterState
{
    protected TPSCharacterController tpsCC;

    public CharacterState(TPSCharacterController _tpsCC)
    {
        tpsCC = _tpsCC;
    }

    public virtual void Enter() { }
    public virtual void Update(InputFrame _inputFrame) { }
    public virtual void Exit() { }
}
