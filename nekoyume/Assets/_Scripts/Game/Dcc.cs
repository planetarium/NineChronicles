using System.Collections.Generic;
using Nekoyume.UI;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Dcc
    {
        public static string IsVisible = "dcc_visible";

        public Dictionary<string, long> Avatars { get; }

        public Dcc(Dictionary<string, long> avatars)
        {
            Avatars = avatars;
        }

        public bool IsActive(out bool isVisible)
        {
            var avatarState = Game.instance.States.CurrentAvatarState;
            var isActive = Avatars.ContainsKey(avatarState.address.ToHex());
            isVisible = PlayerPrefs.GetInt($"{Dcc.IsVisible}_{avatarState.address}", 0) > 0;
            return isActive;
        }

        public void SetVisible(int value)
        {
            var avatarState = Game.instance.States.CurrentAvatarState;
            PlayerPrefs.SetInt($"{Dcc.IsVisible}_{avatarState.address}", value);
        }
    }
}
