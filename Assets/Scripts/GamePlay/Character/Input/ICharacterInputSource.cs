namespace Game.Gameplay.Character
{
    public interface ICharacterInputSource
    {
        bool TryBuildInputContext(out InputContext _inputContext);
    }
}
