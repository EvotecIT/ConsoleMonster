using ConsoleMonster.Input;

namespace ConsoleMonster.Interactive {
    /// <summary>Container gestures are routed ahead of its currently mounted child input.</summary>
    internal interface IAppInputContainer {
        bool HandleContainerInput(InputEvent input, InputControl? focused);
    }
}
