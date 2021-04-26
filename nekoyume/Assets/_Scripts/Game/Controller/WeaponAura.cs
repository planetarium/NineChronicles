using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume;
using UnityEngine;

public static class WeaponAura
{
    private static Dictionary<int, GameObject> _auras;

    private static Dictionary<int, GameObject> Auras
    {
        get
        {
            if (_auras == null)
            {
                _auras = new Dictionary<int, GameObject>();
                var auraRef = Resources.Load<WeaponAuraScriptableObject>(
                    "ScriptableObject/VFX_WeaponAura");

                foreach (var i in auraRef.data)
                {
                    var str = i.name.Split('_');
                    _auras.Add(Convert.ToInt32(str[2]), i);
                }
            }

            return _auras;
        }
    }

    public static GameObject GetAura(int key)
    {
        return Auras.ContainsKey(key) ? Auras[key] : null;
    }




}
