using UnityEngine;
namespace SoulHunter.Core.Services
{

    /// <summary>
    /// Learning Comment:
    /// Yeh script TestService.cs ke core logic ko handle karti hai.
    /// </summary>
    public class TestService : IGameService
    {
        public void Initialize()
        {
            Debug.Log("Test Service Initialize");
        }
    }
}
