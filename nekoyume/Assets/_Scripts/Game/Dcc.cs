using System.Collections.Generic;
using Nekoyume.UI;
using Nekoyume.UI.Model;
using System.Text.Json;
using Libplanet;
using Nekoyume.EnumType;
using UnityEngine;

namespace Nekoyume.Game
{
    public class Dcc
    {
        public static string IsVisible = "dcc_visible";

        public Dictionary<string, int> Avatars { get; }

        public Dcc(Dictionary<string, int> avatars)
        {
            Avatars = avatars;
        }

        public bool IsActive(Address address, out int id, out bool isVisible)
        {
            var hexAddress = address.ToHex();
            var isActive = Avatars.ContainsKey(hexAddress);
            id = Avatars.ContainsKey(hexAddress) ? Avatars[hexAddress] : 0;
            isVisible = PlayerPrefs.GetInt($"{Dcc.IsVisible}_{hexAddress}", 0) > 0;
            return isActive;
        }

        public void SetVisible(int value)
        {
            var avatarState = Game.instance.States.CurrentAvatarState;
            PlayerPrefs.SetInt($"{Dcc.IsVisible}_{avatarState.address}", value);
        }
    }
}
