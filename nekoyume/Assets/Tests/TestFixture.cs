using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Tests
{
    [SetUpFixture]
    public class TestFixture
    {
        [OneTimeSetUp]
        public void Init()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
