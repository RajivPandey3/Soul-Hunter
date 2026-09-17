namespace SoulHunter.Core.Events
{
    /// <summary>
    /// Learning Comment:
    /// Blueprint (event-bus.md) ke hisaab se GameEvent ek interface hona chahiye.
    /// Isse hum Structs use kar payenge jo garbage collection nahi karte.
    /// Ye Canonical Architecture ke sath 100% compliant hai.
    /// </summary>
    public interface IGameEvent
    {
        // Interface body can remain empty. It acts as a constraint marker.
    }
}
